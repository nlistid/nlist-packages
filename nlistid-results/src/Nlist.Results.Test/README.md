cd nlistid-results/src/Nlist.Results.Test

dotnet new mstest -n Nlist.Results.Test -o .
dotnet add package Moq
dotnet add reference ../Nlist.Results/Nlist.Results.csproj
dotnet add reference ../Nlist.Results.Abstractions/Nlist.Results.Abstractions.csproj
