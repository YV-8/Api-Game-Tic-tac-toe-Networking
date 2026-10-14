# Architecture and Design Decisions

> Living document. Each sprint adds to it. Last updated: Sprint 1.

## 1. Overview
<!-- What the system is, who uses it, main goals -->

## 2. Architecture
<!-- Client / server / DB diagram (link to SCRUM-6 diagram) -->
<!-- Where the authoritative game state lives -->

## 3. Components
### 3.1 Client (game-client-pantufla)
### 3.2 Server (game-api-pantufla)
### 3.3 Database (MongoDB)

## 4. Communication protocol
<!-- REST endpoints, WebSocket /ws, message types (link to SCRUM-7 schema) -->
<!-- Payload examples, sequence diagrams (Sprint 3) -->

## 5. Data model
<!-- Collections: users, matches. Fields and indexes -->

## 6. Security
<!-- Authentication, TLS, known risks, mitigations (Sprint 2-3) -->

## 7. Deployment
<!-- Docker, compose, ports, environment variables -->

## 8. Logging and monitoring
<!-- What is logged, where, how to debug the network -->

## 9. Testing
<!-- End-to-end checklist, Wireshark verification (Sprint 4) -->

## 10. Decision log
| Date | Sprint | Decision | Alternatives considered | Reason |
|------|--------|----------|-------------------------|--------|
| | S1 | Plain WebSockets as protocol | MQTT | Simpler for 1v1 turn-based play |
| | S1 | Server written in C# / ASP.NET Core with MongoDB | Python server | Already in legacy code |
| | S1 | Separate repos for client and server | Monorepo | Independent stacks and lifecycles |
| | S1 | TLS planned for Sprint 3 via reverse proxy | Custom TLS in app | Not yet covered in the course |

## 11. Open questions
<!-- Things we have not decided yet -->