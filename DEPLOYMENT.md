# Deploying ResearchRAG for free

The whole stack (frontend, API, worker, MySQL, Qdrant, reverse proxy) runs from
Docker Compose on a single VM. The recommended free host is an **Oracle Cloud
Always Free** ARM VM (4 OCPU / 24 GB RAM, free forever). Any Ubuntu VPS works
the same way from step 3 (e.g. DigitalOcean with GitHub Student Pack credits).

## What the production overlay adds

`docker-compose.prod.yml` puts a Caddy reverse proxy in front of everything:

- `https://your-site/` → frontend, `https://your-site/api/...` → backend
  (same origin, so no CORS or mixed-port issues)
- Automatic HTTPS via Let's Encrypt when `SITE_ADDRESS` is a domain
- Backend and frontend ports are no longer published directly; MySQL and
  Qdrant are bound to `127.0.0.1` only

## 1. Create the VM (Oracle)

1. Sign up at cloud.oracle.com (a card is required for identity verification;
   Always Free resources are never charged). Pick a home region carefully —
   it cannot be changed and A1 capacity varies (Frankfurt/Amsterdam/Marseille
   are usually fine from Egypt).
2. Compute → Instances → Create instance:
   - Image: **Ubuntu 24.04 (aarch64)**
   - Shape: **VM.Standard.A1.Flex**, 4 OCPUs / 24 GB (Always Free eligible)
   - Add your SSH public key
3. Open the firewall in the cloud console: the instance's VCN → Security List
   → Add Ingress Rules for TCP **80** and **443** from `0.0.0.0/0`.

## 2. Prepare the VM

```bash
ssh ubuntu@<VM_IP>

# Docker
curl -fsSL https://get.docker.com | sudo sh
sudo usermod -aG docker $USER && exit   # reconnect for the group to apply

# Oracle Ubuntu images also ship host iptables rules that block 80/443:
ssh ubuntu@<VM_IP>
sudo iptables -I INPUT 6 -p tcp --dport 80 -j ACCEPT
sudo iptables -I INPUT 6 -p tcp --dport 443 -j ACCEPT
sudo netfilter-persistent save
```

## 3. Deploy the app

```bash
git clone <your-repo-url> researchrag && cd researchrag
cp .env.example .env
nano .env
```

Set these in `.env` (everything else can stay default):

```bash
JWT_SIGNING_KEY=<output of: openssl rand -base64 48>
MYSQL_PASSWORD=<strong password>
MYSQL_ROOT_PASSWORD=<strong password>

# frontend calls the API same-origin through Caddy:
API_BASE_URL=

# no demo credentials on a public server:
SEED_DEMO_USERS=false
SEED_ADMIN_EMAIL=you@example.com
SEED_ADMIN_PASSWORD=<strong password>

# with just an IP:
FRONTEND_ORIGIN=http://<VM_IP>
# ...or with a free domain (see step 4):
# SITE_ADDRESS=yourname.duckdns.org
# FRONTEND_ORIGIN=https://yourname.duckdns.org
```

Then:

```bash
docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d --build
```

First build takes a few minutes (images build natively for ARM). The app is
then at `http://<VM_IP>` — register or sign in with your seeded admin.

## 4. Free domain + HTTPS (optional but recommended)

1. Get a free subdomain at duckdns.org and point it at your VM's IP.
2. In `.env` set `SITE_ADDRESS=yourname.duckdns.org` and
   `FRONTEND_ORIGIN=https://yourname.duckdns.org`.
3. `docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d`
   — Caddy obtains and renews the certificate automatically.

## Operations

```bash
# update after code changes
git pull && docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d --build

# logs
docker compose logs -f backend worker

# backup the database volume
docker run --rm -v researchrag_mysql-data:/data -v $PWD:/backup alpine \
  tar czf /backup/mysql-backup.tar.gz -C /data .
```

## Security checklist

- [ ] `JWT_SIGNING_KEY` replaced with a long random value
- [ ] `MYSQL_PASSWORD` / `MYSQL_ROOT_PASSWORD` replaced
- [ ] `SEED_DEMO_USERS=false` and a real `SEED_ADMIN_*` set
- [ ] Only ports 80/443 (and 22 for SSH) reachable from the internet
- [ ] `FRONTEND_ORIGIN` matches the real public origin
