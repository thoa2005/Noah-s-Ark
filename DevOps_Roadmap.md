# DevOps Engineer Roadmap - Ragdoll Game Project

> **Mục tiêu:** Trở thành DevOps Engineer chuyên nghiệp thông qua việc xây dựng và vận hành game ragdoll online.
> **Thời gian:** 12 tháng
> **Approach:** Learn by doing - học từng skill và áp dụng trực tiếp vào project

---

## Checklist Tổng Quan

- [ ] Phase 1: Git & Linux Foundation (Tháng 1)
- [ ] Phase 2: Docker & Containerization (Tháng 2)
- [ ] Phase 3: CI/CD với GitHub Actions (Tháng 3)
- [ ] Phase 4: Networking & Game Server (Tháng 4)
- [ ] Phase 5: Infrastructure as Code - Terraform (Tháng 5)
- [ ] Phase 6: Configuration Management - Ansible (Tháng 6)
- [ ] Phase 7: Kubernetes (Tháng 7-8)
- [ ] Phase 8: Monitoring & Observability (Tháng 9)
- [ ] Phase 9: Security & Secrets Management (Tháng 10)
- [ ] Phase 10: GitOps & Advanced Deployment (Tháng 11)
- [ ] Phase 11: Cost Optimization & FinOps (Tháng 12)

---

## Phase 1: Git & Linux Foundation
**Thời gian:** Tháng 1 | **Status:** ⏳ Pending

### Mục tiêu
- Chuẩn hóa Git workflow theo chuẩn enterprise
- Thành thạo Linux server administration cơ bản
- Setup VPS đầu tiên

### 1.1 Git Advanced
- [ ] Setup GitFlow branching strategy
  ```
  main (production)
  ├── develop (staging)
  ├── feature/* (dev branches)
  ├── release/* (pre-production)
  └── hotfix/* (emergency fixes)
  ```
- [ ] Áp dụng Conventional Commits
  ```
  feat(character): add character selection UI
  fix(ragdoll): resolve physics jitter on spawn
  refactor(network): optimize player sync
  docs(readme): add deployment instructions
  ```
- [ ] Tạo `.gitignore` chuẩn Unity
- [ ] Setup branch protection rules trên GitHub
- [ ] Viết `README.md` với setup instructions
- [ ] Tạo Git hooks (pre-commit chạy tests)

### 1.2 Linux Fundamentals
- [ ] Thuê VPS Ubuntu 22.04 LTS (DigitalOcean $6/month)
- [ ] SSH key setup, disable password auth
- [ ] Học các lệnh cơ bản: ls, cd, grep, find, chmod, chown
- [ ] User management: adduser, usermod, groups
- [ ] Firewall: UFW setup, mở đúng ports
- [ ] Package management: apt install, update, upgrade
- [ ] File permissions: chmod 755, chown deploy:deploy
- [ ] Process management: ps, kill, top, htop
- [ ] Cron jobs: backup tự động, restart định kỳ

### 1.3 Bash Scripting
- [ ] Viết script health-check server
- [ ] Viết script backup database
- [ ] Viết script deploy tự động
- [ ] Systemd service cho game server

### 1.4 Monorepo Structure
- [ ] Tổ chức lại project theo cấu trúc:
  ```
  ragdoll-game/
  ├── .github/workflows/
  ├── infrastructure/
  │   ├── terraform/
  │   ├── ansible/
  │   └── kubernetes/
  ├── Assets/              (Unity project)
  ├── docker/
  ├── scripts/
  └── docs/
  ```

### Resources
- Linux Journey: https://linuxjourney.com
- Git Flow: https://nvie.com/posts/a-successful-git-branching-model
- Conventional Commits: https://www.conventionalcommits.org

### Deliverables
- [ ] VPS Ubuntu đang chạy
- [ ] Git repo với GitFlow setup
- [ ] README.md hoàn chỉnh
- [ ] Bash scripts: health-check.sh, backup.sh, deploy.sh

---

## Phase 2: Docker & Containerization
**Thời gian:** Tháng 2 | **Status:** ⏳ Pending

### Mục tiêu
- Container hóa Unity build và game server
- Hiểu Docker networking, volumes, multi-service

### 2.1 Docker Basics
- [ ] Cài Docker trên VPS Linux
- [ ] Hiểu Dockerfile: FROM, RUN, COPY, EXPOSE, CMD
- [ ] Build image cho Unity project
- [ ] Docker volumes: persistent data
- [ ] Docker networking: bridge, host, overlay

### 2.2 Multi-stage Dockerfile
- [ ] Tạo `docker/Dockerfile.build` (Unity build stage)
- [ ] Tạo `docker/Dockerfile.server` (runtime stage)
- [ ] Optimize image size (dùng alpine base)

### 2.3 Docker Compose
- [ ] Tạo `docker-compose.yml` với các services:
  - game-server
  - postgres (player data)
  - redis (sessions, caching)
  - nginx (reverse proxy)
- [ ] Environment variables với `.env` file
- [ ] Health checks cho từng service
- [ ] Restart policies

### 2.4 Container Registry
- [ ] Setup Docker Hub account
- [ ] Push/pull images
- [ ] Image tagging strategy: `v1.0.0`, `latest`, `sha-abc123`
- [ ] (Optional) Private registry trên VPS

### Resources
- Docker docs: https://docs.docker.com
- Play with Docker: https://labs.play-with-docker.com
- Docker Compose docs: https://docs.docker.com/compose

### Deliverables
- [ ] Dockerfile hoàn chỉnh cho game server
- [ ] docker-compose.yml với 4 services
- [ ] Image đã push lên Docker Hub
- [ ] Game server chạy được trong container

---

## Phase 3: CI/CD với GitHub Actions
**Thời gian:** Tháng 3 | **Status:** ⏳ Pending

### Mục tiêu
- Tự động hóa build, test, deploy
- Mỗi push code → tự động build Unity → tự động deploy

### 3.1 GitHub Actions Basics
- [ ] Hiểu workflow YAML syntax
- [ ] Triggers: push, pull_request, tags, schedule
- [ ] Jobs, steps, actions
- [ ] Secrets management trong GitHub

### 3.2 Unity Build Pipeline
- [ ] Tạo `.github/workflows/build.yml`
  - Trigger: push to develop/main
  - Cache Unity Library folder
  - Run Unity Tests
  - Build cho Windows64, Linux64, WebGL
  - Upload build artifacts
- [ ] Tạo `.github/workflows/deploy.yml`
  - Trigger: push tag `v*.*.*`
  - Build Docker image
  - Push to Docker Hub
  - SSH deploy to VPS

### 3.3 Automated Testing
- [ ] Tạo `Assets/Tests/` folder
- [ ] Viết Unit Tests cho CharacterSelectionManager
- [ ] Viết Integration Tests cho game flow
- [ ] Test results upload to GitHub

### 3.4 Environment Management
- [ ] Tạo 3 environments: dev, staging, production
- [ ] Tạo `Assets/Settings/` với config cho từng env:
  ```
  Settings/
  ├── Dev/PhotonSettings_Dev.asset
  ├── Staging/PhotonSettings_Staging.asset
  └── Production/PhotonSettings_Production.asset
  ```
- [ ] Viết `ConfigManager.cs` để load đúng config theo env

### Resources
- GitHub Actions docs: https://docs.github.com/en/actions
- GameCI (Unity CI/CD): https://game.ci
- Unity Test Framework: https://docs.unity3d.com/Packages/com.unity.test-framework

### Deliverables
- [ ] Auto build chạy khi push code
- [ ] Auto deploy khi push tag
- [ ] Test coverage report
- [ ] 3 environments hoạt động

---

## Phase 4: Networking & Game Server
**Thời gian:** Tháng 4 | **Status:** ⏳ Pending

### Mục tiêu
- Tích hợp Photon PUN2 vào game
- Character selection hoạt động đúng trong multiplayer
- Deploy game server online

### 4.1 Photon PUN2 Integration
- [ ] Cài Photon PUN2 qua Package Manager
- [ ] Setup Photon App ID (dev/staging/prod)
- [ ] Tạo `NetworkManager.cs` trong `Assets/Scripts/Core/`
- [ ] Lobby system: tạo/join room
- [ ] Player spawn với NetworkInstantiate

### 4.2 Character Sync qua Network
- [ ] Cập nhật `CharacterSkinManager.cs` dùng `MonoBehaviourPun`
- [ ] Chỉ apply character cho `photonView.IsMine`
- [ ] Sync character selection qua RPC:
  ```csharp
  [PunRPC]
  void RPC_ApplyCharacter(int characterIndex) { ... }
  ```
- [ ] Test với 2 clients cùng lúc

### 4.3 PlayerPrefs Flow
- [ ] MainMenuScene: chọn character → `PlayerPrefs.SetInt("SelectedCharacterIndex", index)`
- [ ] SampleScene: `CharacterSelectionManager.Start()` → đọc PlayerPrefs → apply character
- [ ] Tạo `CharacterSelectionUI.cs` trong `Assets/Scripts/Player/Customization/`

### 4.4 Network Protocols
- [ ] Hiểu TCP vs UDP (game dùng UDP)
- [ ] Photon Interest Management (chỉ sync player gần nhau)
- [ ] Lag compensation basics
- [ ] Bandwidth optimization

### Resources
- Photon PUN2 docs: https://doc.photonengine.com/pun/current
- Photon free tier: 20 CCU

### Deliverables
- [ ] Multiplayer game chạy được với 2+ players
- [ ] Character selection sync đúng
- [ ] Game server deploy trên VPS

---

## Phase 5: Infrastructure as Code - Terraform
**Thời gian:** Tháng 5 | **Status:** ⏳ Pending

### Mục tiêu
- Provision toàn bộ infrastructure bằng code
- Không bao giờ setup server thủ công nữa

### 5.1 Terraform Basics
- [ ] Cài Terraform
- [ ] Hiểu: providers, resources, variables, outputs
- [ ] State management: local vs remote (S3/Terraform Cloud)
- [ ] `terraform init`, `plan`, `apply`, `destroy`

### 5.2 Provision Game Infrastructure
- [ ] Tạo `infrastructure/terraform/main.tf`:
  - DigitalOcean Droplet (game server)
  - Firewall rules (ports 22, 80, 443, 7777/udp)
  - Load Balancer
  - DNS records
- [ ] Tạo `infrastructure/terraform/variables.tf`
- [ ] Tạo `infrastructure/terraform/outputs.tf`
- [ ] Remote state backend (Terraform Cloud free tier)

### 5.3 Terraform Modules
- [ ] Module cho networking
- [ ] Module cho compute (droplets)
- [ ] Module cho monitoring

### 5.4 Terraform trong CI/CD
- [ ] Thêm Terraform plan vào GitHub Actions
- [ ] Auto apply khi merge to main
- [ ] Terraform Cloud workspace

### Resources
- Terraform docs: https://developer.hashicorp.com/terraform
- DigitalOcean Terraform provider: https://registry.terraform.io/providers/digitalocean/digitalocean

### Deliverables
- [ ] Toàn bộ infrastructure được define bằng code
- [ ] `terraform apply` tạo được server từ đầu
- [ ] State lưu trên remote backend

---

## Phase 6: Configuration Management - Ansible
**Thời gian:** Tháng 6 | **Status:** ⏳ Pending

### Mục tiêu
- Tự động configure server sau khi Terraform tạo
- Không bao giờ SSH vào server để cài thủ công

### 6.1 Ansible Basics
- [ ] Cài Ansible
- [ ] Inventory files: hosts, groups, variables
- [ ] Playbooks: tasks, handlers, roles
- [ ] Modules: apt, copy, template, service, cron

### 6.2 Game Server Playbook
- [ ] Tạo `infrastructure/ansible/playbook.yml`:
  - Install Docker, Nginx, Certbot
  - Create deploy user
  - Copy docker-compose.yml
  - Setup SSL với Let's Encrypt
  - Setup cron jobs (backup, SSL renewal)
  - Start game server service

### 6.3 Ansible Roles
- [ ] Role: common (base server setup)
- [ ] Role: docker (Docker installation)
- [ ] Role: nginx (web server config)
- [ ] Role: game-server (game-specific setup)

### 6.4 Ansible Vault
- [ ] Encrypt sensitive variables (passwords, API keys)
- [ ] `ansible-vault encrypt_string 'secret' --name 'db_password'`

### Resources
- Ansible docs: https://docs.ansible.com
- Ansible Galaxy (community roles): https://galaxy.ansible.com

### Deliverables
- [ ] `ansible-playbook playbook.yml` setup server hoàn toàn tự động
- [ ] SSL certificate tự động renew
- [ ] Secrets được encrypt

---

## Phase 7: Kubernetes
**Thời gian:** Tháng 7-8 | **Status:** ⏳ Pending

### Mục tiêu
- Container orchestration cho game server
- Auto-scaling khi nhiều player
- Zero-downtime deployments

### 7.1 Kubernetes Basics
- [ ] Cài kubectl, minikube (local dev)
- [ ] Hiểu: Pods, Deployments, Services, ConfigMaps, Secrets
- [ ] `kubectl get`, `describe`, `logs`, `exec`
- [ ] YAML manifests

### 7.2 Deploy Game lên K8s
- [ ] Tạo `infrastructure/kubernetes/deployment.yml`
- [ ] Tạo `infrastructure/kubernetes/service.yml`
- [ ] ConfigMap cho game config
- [ ] Secrets cho API keys
- [ ] Liveness & Readiness probes

### 7.3 Auto-scaling
- [ ] HorizontalPodAutoscaler (HPA)
  - Min: 2 replicas
  - Max: 10 replicas
  - Scale up khi CPU > 70%
- [ ] Resource requests & limits

### 7.4 Helm Charts
- [ ] Tạo Helm chart cho game
- [ ] Values files cho từng environment
- [ ] `helm install`, `upgrade`, `rollback`

### 7.5 Managed K8s
- [ ] DigitalOcean Kubernetes (DOKS)
- [ ] Deploy lên managed cluster
- [ ] kubectl context management

### Resources
- Kubernetes docs: https://kubernetes.io/docs
- KodeKloud (free K8s labs): https://kodekloud.com
- Helm docs: https://helm.sh/docs

### Deliverables
- [ ] Game chạy trên K8s cluster
- [ ] Auto-scaling hoạt động
- [ ] Helm chart hoàn chỉnh
- [ ] Zero-downtime deployment

---

## Phase 8: Monitoring & Observability
**Thời gian:** Tháng 9 | **Status:** ⏳ Pending

### Mục tiêu
- Biết game đang hoạt động thế nào 24/7
- Alert khi có vấn đề
- Debug production issues nhanh

### 8.1 Prometheus
- [ ] Deploy Prometheus trên K8s
- [ ] Scrape metrics từ game server
- [ ] Custom metrics trong Unity:
  - `game_active_players` (Gauge)
  - `game_player_connections_total` (Counter)
  - `game_frame_time_seconds` (Histogram)
  - `game_match_duration_seconds` (Summary)
- [ ] Alert rules: server down, high CPU, low players

### 8.2 Grafana
- [ ] Deploy Grafana
- [ ] Tạo dashboards:
  - Server health (CPU, RAM, Network)
  - Game metrics (CCU, matches, latency)
  - Business metrics (DAU, retention)
- [ ] Alert notifications (Discord/Slack/Email)

### 8.3 ELK Stack
- [ ] Elasticsearch: lưu logs
- [ ] Logstash: parse và transform logs
- [ ] Kibana: visualize logs
- [ ] Structured logging trong Unity:
  ```csharp
  Debug.Log(JsonUtility.ToJson(new LogEntry {
      level = "INFO",
      message = "Player connected",
      playerId = photonView.Owner.UserId,
      timestamp = DateTime.UtcNow
  }));
  ```

### 8.4 Distributed Tracing
- [ ] Jaeger hoặc Zipkin
- [ ] Trace request từ client → server → database

### Resources
- Prometheus docs: https://prometheus.io/docs
- Grafana docs: https://grafana.com/docs
- ELK Stack: https://www.elastic.co/guide

### Deliverables
- [ ] Grafana dashboard public (portfolio)
- [ ] Alert khi server down
- [ ] Log aggregation hoạt động
- [ ] SLO dashboard (uptime, latency)

---

## Phase 9: Security & Secrets Management
**Thời gian:** Tháng 10 | **Status:** ⏳ Pending

### Mục tiêu
- Bảo mật infrastructure và application
- Không bao giờ hardcode secrets

### 9.1 HashiCorp Vault
- [ ] Cài và configure Vault
- [ ] Lưu tất cả secrets vào Vault:
  - Photon App ID/Secret
  - Database credentials
  - API keys
- [ ] Dynamic secrets (tự động rotate)
- [ ] Integrate với K8s

### 9.2 SSL/TLS
- [ ] Let's Encrypt certificate tự động
- [ ] HTTPS cho tất cả endpoints
- [ ] Certificate rotation

### 9.3 Network Security
- [ ] Cloudflare DDoS protection (free tier)
- [ ] Rate limiting trên Nginx
- [ ] VPN cho admin access (WireGuard)
- [ ] Network policies trong K8s

### 9.4 Application Security
- [ ] Anti-cheat cơ bản (server-side validation)
- [ ] Input validation
- [ ] JWT authentication cho REST API
- [ ] OWASP Top 10 awareness

### 9.5 Security Scanning
- [ ] Trivy: scan Docker images
- [ ] Dependabot: auto update dependencies
- [ ] SAST trong CI/CD pipeline

### Resources
- Vault docs: https://developer.hashicorp.com/vault
- OWASP: https://owasp.org
- Cloudflare: https://cloudflare.com

### Deliverables
- [ ] Vault running, tất cả secrets được manage
- [ ] SSL trên tất cả endpoints
- [ ] Security scan trong CI/CD
- [ ] DDoS protection active

---

## Phase 10: GitOps & Advanced Deployment
**Thời gian:** Tháng 11 | **Status:** ⏳ Pending

### Mục tiêu
- Infrastructure và deployment hoàn toàn declarative
- Git là single source of truth

### 10.1 ArgoCD
- [ ] Cài ArgoCD trên K8s cluster
- [ ] Tạo Application manifest
- [ ] Auto-sync khi push to main
- [ ] Rollback bằng git revert

### 10.2 Blue-Green Deployment
- [ ] Setup 2 environments: blue (current) và green (new)
- [ ] Switch traffic từ blue sang green
- [ ] Rollback instant nếu có vấn đề

### 10.3 Canary Releases
- [ ] Deploy version mới cho 10% players trước
- [ ] Monitor metrics
- [ ] Gradually increase traffic
- [ ] Auto rollback nếu error rate tăng

### 10.4 Feature Flags
- [ ] Implement feature flags trong game
- [ ] Enable/disable features không cần deploy
- [ ] A/B testing cho game mechanics

### Resources
- ArgoCD docs: https://argo-cd.readthedocs.io
- Flagger (canary): https://flagger.app

### Deliverables
- [ ] ArgoCD dashboard
- [ ] Blue-green deployment hoạt động
- [ ] Canary release pipeline
- [ ] Feature flags system

---

## Phase 11: Cost Optimization & FinOps
**Thời gian:** Tháng 12 | **Status:** ⏳ Pending

### Mục tiêu
- Tối ưu chi phí infrastructure
- Biết đang tiêu tiền vào đâu

### 11.1 Resource Right-sizing
- [ ] Analyze CPU/RAM usage thực tế
- [ ] Downsize instances không cần thiết
- [ ] Spot instances cho non-critical workloads

### 11.2 Auto-scaling Policies
- [ ] Scale down ban đêm (ít player)
- [ ] Scale up giờ cao điểm
- [ ] Scheduled scaling

### 11.3 Cost Monitoring
- [ ] DigitalOcean billing alerts
- [ ] Cost per player metric
- [ ] Monthly cost report tự động

### 11.4 Optimization Techniques
- [ ] Docker image optimization (giảm size)
- [ ] Database query optimization
- [ ] CDN cho static assets (Cloudflare free)
- [ ] Compression (gzip, brotli)

### Deliverables
- [ ] Monthly cost < $50 cho 100 CCU
- [ ] Cost dashboard trong Grafana
- [ ] Auto-scaling theo giờ

---

## Certification Path

Sau 12 tháng, chuẩn bị cho:

| Certification | Thời gian | Chi phí |
|---------------|-----------|---------|
| AWS Certified Cloud Practitioner | Tháng 13 | $100 |
| HashiCorp Terraform Associate | Tháng 14 | $70 |
| Certified Kubernetes Administrator (CKA) | Tháng 15 | $395 |
| AWS Certified DevOps Engineer - Professional | Tháng 16 | $300 |

---

## Portfolio Showcase

Sau project này, bạn có thể show:

- [ ] GitHub repo với CI/CD pipelines hoàn chỉnh
- [ ] Live game đang chạy trên production
- [ ] Grafana dashboard public (metrics)
- [ ] Architecture diagram (draw.io)
- [ ] Blog posts về từng phase
- [ ] YouTube videos demo deployment process

---

## Tech Stack Summary

| Category | Tools | Status |
|----------|-------|--------|
| OS | Ubuntu 22.04 LTS | ⏳ |
| Version Control | Git, GitHub, GitFlow | ⏳ |
| CI/CD | GitHub Actions, GameCI | ⏳ |
| Containerization | Docker, Docker Compose | ⏳ |
| Orchestration | Kubernetes, Helm | ⏳ |
| IaC | Terraform | ⏳ |
| Config Management | Ansible | ⏳ |
| Monitoring | Prometheus, Grafana | ⏳ |
| Logging | ELK Stack | ⏳ |
| Security | Vault, Cloudflare, Trivy | ⏳ |
| GitOps | ArgoCD | ⏳ |
| Cloud | DigitalOcean | ⏳ |
| Game Networking | Photon PUN2 | ⏳ |
| Database | PostgreSQL, Redis | ⏳ |

---

## Notes & Progress Log

### Tháng 1
- 

### Tháng 2
- 

### Tháng 3
- 

*(Cập nhật khi hoàn thành từng task)*

---

*Last updated: 2026-05-27*
