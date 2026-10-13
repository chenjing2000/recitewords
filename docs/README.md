# ReciteWords documentation

These pages describe ReciteWords. File formats and architecture remain separate reference pages; testing and publishing commands use the unified tools.

| Document | Responsibility |
| --- | --- |
| [Getting Started](Getting-Started.md) | Runtime requirements, launch, and first use |
| [Application Overview](Application-Overview.md) | Purpose, modules, and application-specific architecture |
| [Configuration](Configuration.md) | Settings location, ownership, preservation, and recovery |
| [Testing](Testing.md) | Core/UI/Full entry points, media preparation, and published acceptance |
| [Publishing](Publishing.md) | Portable profile, staged updates, and GitHub Release workflow |
| [Release Notes](Release-Notes.md) | Distribution changes and runtime requirements |
| [Health Check](Health-Check-2026-10-13.md) | Dated verification evidence and limitations |
| [Code Review](Code-Review-2026-10-13.md) | Current module assessment, local fixes, retained design, and verified scope |

## Application-specific references

- [User Guide](User-Guide.md)

- [Word-list format](skills/recitewords-wordlist-format/SKILL.md)
- [Review progress](Progress-Format.md)
- [Audio naming](Audio-Format.md)
- [Module design](Module-Design.md)

## Historical references

- [Previous code review](Code-Review.md)
- [Design purpose](Design-Purpose.md)
- [Format upgrade](Format-Upgrade.md)
- [Implementation report](Implementation-Report.md)
- [Original implementation plan](superpowers/plans/2026-10-04-recitewords-implementation-plan.md)

## Maintenance rules

Keep executable commands in Testing and Publishing; link them from usage guides. Keep settings rules in Configuration. Record dated results in Health Check rather than implying that an old result verifies later changes. Domain references describe file formats and behavior, not another publishing pipeline. Historical audits and plans remain historical and are not the current workflow entry point.
