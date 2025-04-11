# 404-Found-DAM

## Installation Documentation (Dev Environment)

### Install necessary dependencies

To install the necessary dependencies please run the setup script from the root directory:

- for MAC
  ```bash
  ./setup.sh
  ```

- for Windows
  ```bash
  .\setup.ps1
  ```

For Windows, if you see a script execution error then also run this and try again:

```bash
Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass
```

For both mac/windows you will need to add a path declaration in either your .bashrc or .zshrc for dotnet-ef tool (instruction given at the end of the setup script)

### Run backend and mysql database
See the README.md file in the dotnet-api/ folder

### Run frontend
See the README.md file in the frontend/ folder

## Installation Documentation (Test Environment)
For testing we used the same dev environment as well as the WIP link to do manual and qa testing.

## Installation Documentation (Production Environment)
For the production environment see the release documentation in the final project submission folder.
