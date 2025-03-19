### Requirements:
- .NET 8

## Getting Started
Navigate to the `dotnet-backend` directory:
```bash
cd dotnet-backend
```
Execute the following to install packages
```bash
dotnet restore
```

To run the API, navigate to the `APIs` directory and use the following commands to build and start the application:
```bash
cd APIs

dotnet run
```

### Cheatsheet:
- visit 'http://localhost:5155/swagger' to manually try and test.


Every week or so, rebase to main.
1. commit your current files
2. call git status and verify that the message reads as: your branch is up to date with 'origin/<your branch name>'. nothing to commit, working tree clean
3. git checkout main
4. git pull
5. git checkout <your branch name>
6. git rebase main
7. resolve merge conflicts, if any. then if there were merge conflicts, run git rebase --continue

Every week or so, update your branch with the changes from main. Using GitHub Desktop,
1. Go to your branch.
2. Click "Branch" (in the top bar), then "Update from main"