# Nlist.Results.Test

MSTest + Moq tests for `Nlist.Results`. It currently holds only the template's empty `Test1`
placeholder. See [SPEC.md](../../SPEC.md) §8 for scope.

```bash
cd nlistid-results/src/Nlist.Results.Test
dotnet test
```

The project was scaffolded with:

```bash
dotnet new mstest -n Nlist.Results.Test -o .
dotnet add package Moq
dotnet add reference ../Nlist.Results/Nlist.Results.csproj
dotnet add reference ../Nlist.Results.Abstractions/Nlist.Results.Abstractions.csproj
```
