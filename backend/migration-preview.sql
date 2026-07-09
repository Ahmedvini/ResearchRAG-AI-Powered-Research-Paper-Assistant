CREATE TABLE IF NOT EXISTS `__EFMigrationsHistory` (
    `MigrationId` varchar(150) CHARACTER SET utf8mb4 NOT NULL,
    `ProductVersion` varchar(32) CHARACTER SET utf8mb4 NOT NULL,
    CONSTRAINT `PK___EFMigrationsHistory` PRIMARY KEY (`MigrationId`)
) CHARACTER SET=utf8mb4;

START TRANSACTION;
ALTER DATABASE CHARACTER SET utf8mb4;

CREATE TABLE `QueryLogs` (
    `Id` char(36) COLLATE ascii_general_ci NOT NULL,
    `UserId` char(36) COLLATE ascii_general_ci NOT NULL,
    `WorkspaceId` char(36) COLLATE ascii_general_ci NOT NULL,
    `Query` longtext CHARACTER SET utf8mb4 NOT NULL,
    `RetrievedChunks` int NOT NULL,
    `LatencyMs` int NOT NULL,
    `CreatedAt` datetime(6) NOT NULL,
    `UpdatedAt` datetime(6) NULL,
    CONSTRAINT `PK_QueryLogs` PRIMARY KEY (`Id`)
) CHARACTER SET=utf8mb4;

CREATE TABLE `Users` (
    `Id` char(36) COLLATE ascii_general_ci NOT NULL,
    `Email` varchar(320) CHARACTER SET utf8mb4 NOT NULL,
    `PasswordHash` varchar(512) CHARACTER SET utf8mb4 NOT NULL,
    `DisplayName` longtext CHARACTER SET utf8mb4 NOT NULL,
    `EmailVerified` tinyint(1) NOT NULL,
    `Role` varchar(32) CHARACTER SET utf8mb4 NOT NULL,
    `CreatedAt` datetime(6) NOT NULL,
    `UpdatedAt` datetime(6) NULL,
    CONSTRAINT `PK_Users` PRIMARY KEY (`Id`)
) CHARACTER SET=utf8mb4;

CREATE TABLE `OneTimeTokens` (
    `Id` char(36) COLLATE ascii_general_ci NOT NULL,
    `UserId` char(36) COLLATE ascii_general_ci NOT NULL,
    `TokenHash` varchar(128) CHARACTER SET utf8mb4 NOT NULL,
    `Purpose` varchar(64) CHARACTER SET utf8mb4 NOT NULL,
    `ExpiresAt` datetime(6) NOT NULL,
    `UsedAt` datetime(6) NULL,
    `CreatedAt` datetime(6) NOT NULL,
    `UpdatedAt` datetime(6) NULL,
    CONSTRAINT `PK_OneTimeTokens` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_OneTimeTokens_Users_UserId` FOREIGN KEY (`UserId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE TABLE `RefreshTokens` (
    `Id` char(36) COLLATE ascii_general_ci NOT NULL,
    `UserId` char(36) COLLATE ascii_general_ci NOT NULL,
    `TokenHash` varchar(128) CHARACTER SET utf8mb4 NOT NULL,
    `ExpiresAt` datetime(6) NOT NULL,
    `RevokedAt` datetime(6) NULL,
    `ReplacedByTokenHash` varchar(128) CHARACTER SET utf8mb4 NULL,
    `CreatedAt` datetime(6) NOT NULL,
    `UpdatedAt` datetime(6) NULL,
    CONSTRAINT `PK_RefreshTokens` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_RefreshTokens_Users_UserId` FOREIGN KEY (`UserId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE TABLE `Workspaces` (
    `Id` char(36) COLLATE ascii_general_ci NOT NULL,
    `UserId` char(36) COLLATE ascii_general_ci NOT NULL,
    `Name` varchar(200) CHARACTER SET utf8mb4 NOT NULL,
    `Description` longtext CHARACTER SET utf8mb4 NOT NULL,
    `CreatedAt` datetime(6) NOT NULL,
    `UpdatedAt` datetime(6) NULL,
    CONSTRAINT `PK_Workspaces` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_Workspaces_Users_UserId` FOREIGN KEY (`UserId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE TABLE `ChatSessions` (
    `Id` char(36) COLLATE ascii_general_ci NOT NULL,
    `WorkspaceId` char(36) COLLATE ascii_general_ci NOT NULL,
    `Title` longtext CHARACTER SET utf8mb4 NOT NULL,
    `CreatedAt` datetime(6) NOT NULL,
    `UpdatedAt` datetime(6) NULL,
    CONSTRAINT `PK_ChatSessions` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_ChatSessions_Workspaces_WorkspaceId` FOREIGN KEY (`WorkspaceId`) REFERENCES `Workspaces` (`Id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE TABLE `Documents` (
    `Id` char(36) COLLATE ascii_general_ci NOT NULL,
    `WorkspaceId` char(36) COLLATE ascii_general_ci NOT NULL,
    `OriginalFileName` longtext CHARACTER SET utf8mb4 NOT NULL,
    `StoredFileName` longtext CHARACTER SET utf8mb4 NOT NULL,
    `StoragePath` longtext CHARACTER SET utf8mb4 NOT NULL,
    `SizeBytes` bigint NOT NULL,
    `Status` varchar(32) CHARACTER SET utf8mb4 NOT NULL,
    `FailureReason` longtext CHARACTER SET utf8mb4 NULL,
    `Title` longtext CHARACTER SET utf8mb4 NULL,
    `Authors` longtext CHARACTER SET utf8mb4 NULL,
    `PublicationYear` int NULL,
    `Abstract` longtext CHARACTER SET utf8mb4 NULL,
    `Keywords` longtext CHARACTER SET utf8mb4 NULL,
    `CreatedAt` datetime(6) NOT NULL,
    `UpdatedAt` datetime(6) NULL,
    CONSTRAINT `PK_Documents` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_Documents_Workspaces_WorkspaceId` FOREIGN KEY (`WorkspaceId`) REFERENCES `Workspaces` (`Id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE TABLE `ChatMessages` (
    `Id` char(36) COLLATE ascii_general_ci NOT NULL,
    `ChatSessionId` char(36) COLLATE ascii_general_ci NOT NULL,
    `Role` longtext CHARACTER SET utf8mb4 NOT NULL,
    `Content` longtext CHARACTER SET utf8mb4 NOT NULL,
    `CreatedAt` datetime(6) NOT NULL,
    `UpdatedAt` datetime(6) NULL,
    CONSTRAINT `PK_ChatMessages` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_ChatMessages_ChatSessions_ChatSessionId` FOREIGN KEY (`ChatSessionId`) REFERENCES `ChatSessions` (`Id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE TABLE `DocumentChunks` (
    `Id` char(36) COLLATE ascii_general_ci NOT NULL,
    `DocumentId` char(36) COLLATE ascii_general_ci NOT NULL,
    `WorkspaceId` char(36) COLLATE ascii_general_ci NOT NULL,
    `Text` longtext CHARACTER SET utf8mb4 NOT NULL,
    `PageNumber` int NOT NULL,
    `SectionName` longtext CHARACTER SET utf8mb4 NOT NULL,
    `VectorId` longtext CHARACTER SET utf8mb4 NOT NULL,
    `CreatedAt` datetime(6) NOT NULL,
    `UpdatedAt` datetime(6) NULL,
    CONSTRAINT `PK_DocumentChunks` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_DocumentChunks_Documents_DocumentId` FOREIGN KEY (`DocumentId`) REFERENCES `Documents` (`Id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE TABLE `PaperExtractions` (
    `Id` char(36) COLLATE ascii_general_ci NOT NULL,
    `DocumentId` char(36) COLLATE ascii_general_ci NOT NULL,
    `PaperTitle` longtext CHARACTER SET utf8mb4 NOT NULL,
    `AuthorsJson` longtext CHARACTER SET utf8mb4 NOT NULL,
    `Dataset` longtext CHARACTER SET utf8mb4 NOT NULL,
    `Model` longtext CHARACTER SET utf8mb4 NOT NULL,
    `MetricsJson` longtext CHARACTER SET utf8mb4 NOT NULL,
    `Accuracy` longtext CHARACTER SET utf8mb4 NOT NULL,
    `LimitationsJson` longtext CHARACTER SET utf8mb4 NOT NULL,
    `FutureWorkJson` longtext CHARACTER SET utf8mb4 NOT NULL,
    `CreatedAt` datetime(6) NOT NULL,
    `UpdatedAt` datetime(6) NULL,
    CONSTRAINT `PK_PaperExtractions` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_PaperExtractions_Documents_DocumentId` FOREIGN KEY (`DocumentId`) REFERENCES `Documents` (`Id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE TABLE `ProcessingJobs` (
    `Id` char(36) COLLATE ascii_general_ci NOT NULL,
    `DocumentId` char(36) COLLATE ascii_general_ci NOT NULL,
    `Status` varchar(32) CHARACTER SET utf8mb4 NOT NULL,
    `Attempts` int NOT NULL,
    `LastError` longtext CHARACTER SET utf8mb4 NULL,
    `StartedAt` datetime(6) NULL,
    `CompletedAt` datetime(6) NULL,
    `CreatedAt` datetime(6) NOT NULL,
    `UpdatedAt` datetime(6) NULL,
    CONSTRAINT `PK_ProcessingJobs` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_ProcessingJobs_Documents_DocumentId` FOREIGN KEY (`DocumentId`) REFERENCES `Documents` (`Id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE TABLE `Citations` (
    `Id` char(36) COLLATE ascii_general_ci NOT NULL,
    `ChatMessageId` char(36) COLLATE ascii_general_ci NOT NULL,
    `ChunkId` char(36) COLLATE ascii_general_ci NOT NULL,
    `DocumentName` longtext CHARACTER SET utf8mb4 NOT NULL,
    `Section` longtext CHARACTER SET utf8mb4 NOT NULL,
    `PageNumber` int NOT NULL,
    `RelevanceScore` double NOT NULL,
    `CreatedAt` datetime(6) NOT NULL,
    `UpdatedAt` datetime(6) NULL,
    CONSTRAINT `PK_Citations` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_Citations_ChatMessages_ChatMessageId` FOREIGN KEY (`ChatMessageId`) REFERENCES `ChatMessages` (`Id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE INDEX `IX_ChatMessages_ChatSessionId` ON `ChatMessages` (`ChatSessionId`);

CREATE INDEX `IX_ChatSessions_WorkspaceId` ON `ChatSessions` (`WorkspaceId`);

CREATE INDEX `IX_Citations_ChatMessageId` ON `Citations` (`ChatMessageId`);

CREATE INDEX `IX_DocumentChunks_DocumentId` ON `DocumentChunks` (`DocumentId`);

CREATE INDEX `IX_DocumentChunks_WorkspaceId_DocumentId` ON `DocumentChunks` (`WorkspaceId`, `DocumentId`);

CREATE INDEX `IX_Documents_WorkspaceId_Status` ON `Documents` (`WorkspaceId`, `Status`);

CREATE UNIQUE INDEX `IX_OneTimeTokens_TokenHash_Purpose` ON `OneTimeTokens` (`TokenHash`, `Purpose`);

CREATE INDEX `IX_OneTimeTokens_UserId` ON `OneTimeTokens` (`UserId`);

CREATE UNIQUE INDEX `IX_PaperExtractions_DocumentId` ON `PaperExtractions` (`DocumentId`);

CREATE INDEX `IX_ProcessingJobs_DocumentId` ON `ProcessingJobs` (`DocumentId`);

CREATE INDEX `IX_ProcessingJobs_Status` ON `ProcessingJobs` (`Status`);

CREATE UNIQUE INDEX `IX_RefreshTokens_TokenHash` ON `RefreshTokens` (`TokenHash`);

CREATE INDEX `IX_RefreshTokens_UserId` ON `RefreshTokens` (`UserId`);

CREATE UNIQUE INDEX `IX_Users_Email` ON `Users` (`Email`);

CREATE UNIQUE INDEX `IX_Workspaces_UserId_Name` ON `Workspaces` (`UserId`, `Name`);

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20260709183251_InitialCreate', '9.0.0');

COMMIT;

