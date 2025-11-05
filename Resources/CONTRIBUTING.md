# Contributing to Secure Stamp Service

Thank you for your interest in contributing to the Secure Stamp Service!

## Code of Conduct

Be respectful and professional in all interactions.

## How to Contribute

1. **Fork the repository**
2. **Create a feature branch**: `git checkout -b feature/your-feature-name`
3. **Make your changes**
4. **Run tests**: `dotnet test`
5. **Commit your changes**: `git commit -am 'Add some feature'`
6. **Push to the branch**: `git push origin feature/your-feature-name`
7. **Submit a Pull Request**

## Development Setup

See `BUILD.md` for detailed build instructions.

Quick start:
```powershell
dotnet restore
dotnet build
dotnet test
```

## Testing

- All new features should include unit tests
- Run the full test suite before submitting a PR
- Aim for high code coverage

## Security

- **Never commit private keys, certificates, or credentials**
- Report security vulnerabilities privately to the maintainers
- Follow secure coding practices

## Pull Request Guidelines

- Keep PRs focused on a single feature or fix
- Update documentation as needed
- Add tests for new functionality
- Ensure all tests pass
- Update CHANGELOG if applicable

## Code Style

- Follow C# coding conventions
- Use meaningful variable and method names
- Add XML documentation comments for public APIs
- Keep methods focused and concise

## Questions?

Open an issue for questions or discussion.

## License

By contributing, you agree that your contributions will be licensed under the MIT License.
