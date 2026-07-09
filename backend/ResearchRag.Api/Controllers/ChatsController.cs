using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ResearchRag.Application.Abstractions;
using ResearchRag.Application.Chats;
using ResearchRag.Domain.Entities;
using ResearchRag.Infrastructure.Persistence;

namespace ResearchRag.Api.Controllers;

[Authorize]
public sealed class ChatsController(AppDbContext db, IRagAnswerService rag) : ApiControllerBase
{
    [HttpGet("workspace/{workspaceId:guid}")]
    public async Task<IReadOnlyList<ChatDto>> List(Guid workspaceId, CancellationToken cancellationToken)
    {
        return await db.ChatSessions
            .Where(x => x.WorkspaceId == workspaceId && x.Workspace!.UserId == CurrentUserId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new ChatDto(x.Id, x.WorkspaceId, x.Title, x.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    [HttpPost]
    public async Task<ActionResult<ChatDto>> Create(CreateChatRequest request, CancellationToken cancellationToken)
    {
        var ownsWorkspace = await db.Workspaces.AnyAsync(x => x.Id == request.WorkspaceId && x.UserId == CurrentUserId, cancellationToken);
        if (!ownsWorkspace) return NotFound("Workspace not found.");

        var title = string.IsNullOrWhiteSpace(request.Title) ? "Research chat" : request.Title.Trim();
        var chat = new ChatSession { WorkspaceId = request.WorkspaceId, Title = title };
        db.ChatSessions.Add(chat);
        await db.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(Messages), new { id = chat.Id }, new ChatDto(chat.Id, chat.WorkspaceId, chat.Title, chat.CreatedAt));
    }

    [HttpGet("{id:guid}/messages")]
    public async Task<IReadOnlyList<ChatMessageDto>> Messages(Guid id, CancellationToken cancellationToken)
    {
        return await db.ChatMessages
            .Where(x => x.ChatSessionId == id && x.ChatSession!.Workspace!.UserId == CurrentUserId)
            .OrderBy(x => x.CreatedAt)
            .Select(x => new ChatMessageDto(
                x.Id,
                x.Role,
                x.Content,
                x.Citations.Select(c => new CitationDto(c.ChunkId, c.DocumentName, c.Section, c.PageNumber, c.RelevanceScore)).ToList(),
                x.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    [HttpPost("{id:guid}/messages")]
    public async Task<ActionResult<RagAnswerDto>> Send(Guid id, SendMessageRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Question)) return BadRequest("Question must not be empty.");
        var chat = await db.ChatSessions.Include(x => x.Workspace).SingleOrDefaultAsync(x => x.Id == id && x.Workspace!.UserId == CurrentUserId, cancellationToken);
        if (chat is null) return NotFound("Chat not found.");

        var stopwatch = Stopwatch.StartNew();
        var userMessage = new ChatMessage { ChatSessionId = id, Role = "user", Content = request.Question };
        db.ChatMessages.Add(userMessage);

        var answer = await rag.AnswerAsync(chat.WorkspaceId, request.DocumentIds, request.Question, cancellationToken);
        var assistantMessage = new ChatMessage { ChatSessionId = id, Role = "assistant", Content = answer.Answer };
        assistantMessage.Citations = answer.Citations.Select(c => new Citation
        {
            ChatMessage = assistantMessage,
            ChunkId = c.ChunkId,
            DocumentName = c.DocumentName,
            Section = c.Section,
            PageNumber = c.PageNumber,
            RelevanceScore = c.RelevanceScore
        }).ToList();
        db.ChatMessages.Add(assistantMessage);
        db.QueryLogs.Add(new QueryLog
        {
            UserId = CurrentUserId,
            WorkspaceId = chat.WorkspaceId,
            Query = request.Question,
            RetrievedChunks = answer.Citations.Count,
            LatencyMs = (int)stopwatch.ElapsedMilliseconds
        });

        await db.SaveChangesAsync(cancellationToken);
        return answer;
    }
}

