# gari-kaagada

A letter-writing service built around user anonymity. See `.specify/memory/constitution.md`
for the project's governing principles, and `AGENTS.md` for a condensed, agent-facing summary
of the technical standards.

## Prerequisites

- .NET SDK matching the Aspire AppHost's required TFM (`dotnet --version`).
- Node.js + npm (for `gari-kagada-client`).
- [Podman](https://podman.io/) ≥ 5.0.0, with `ASPIRE_CONTAINER_RUNTIME=podman` exported in your
  shell profile. Docker is intentionally **not** required or supported (constitution Principle
  II — self-hosted sovereignty).
- Run `aspire doctor` after cloning as a first sanity check — it should report Podman as the
  active, configured container runtime before you run anything else.

## Running the full stack locally

```sh
aspire run
```

This starts every project and backing container (PostgreSQL, Keycloak, the SigNoz observability
stack, and the `gari-kagada-client` frontend) as one application graph via
`GariKaagada.AppHost`. See `specs/001-project-scaffolding/quickstart.md` for a full validation
walkthrough.
