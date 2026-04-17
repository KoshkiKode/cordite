# Security Policy

## Supported Versions

Only the latest stable release receives security updates.

| Version | Supported          |
| ------- | ------------------ |
| 1.x.x   | :white_check_mark: |
| < 1.0   | :x:                |

## Reporting a Vulnerability

**Please do not file public GitHub issues for security vulnerabilities.**

Report vulnerabilities privately using [GitHub's private vulnerability reporting](https://github.com/KoshkiKode/cordite/security/advisories/new).

Include:
- A clear description of the vulnerability and its potential impact
- Steps to reproduce
- Any suggested mitigation or patch (optional but appreciated)

You can expect an initial response within **72 hours**. If the vulnerability is
confirmed, a patch will be prepared and a new release issued. You will be
credited in the release notes unless you prefer to remain anonymous.

## Security Measures

- All GitHub Actions run with **minimum required permissions** (`contents: read` by default)
- **Dependabot** is enabled for automated dependency updates (GitHub Actions + NuGet)
- **CodeQL** static analysis runs on every push and pull request to `main`
- Android release builds are signed with a keystore stored as an encrypted GitHub secret
- macOS builds support optional notarization via Apple credentials stored as secrets
