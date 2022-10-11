# visualstudio-extension
Codiga extension for Visual Studio for the fastest code analysis and code snippets management

![example workflow](https://github.com/codiga/visualstudio-extension/actions/workflows/main.yml/badge.svg)

# Debug with the VS experimental instance 
This section describes how you can build and debug the extension experimental instance of Visual Studio.

### Prerequisites
* Installation of Visual Studio ([Visual Studio 2022](https://visualstudio.microsoft.com/vs/) is recommended)

### Start the experimental instance
1. Open `Extension.sln` with Visual Studio
2. Hit `Ctrl + F5` to bring up the experimental instance

The experimental instance will have the extension automatically installed and updated.

## Test out the extension
To see how the extension is working create or open a `.cs`-File and type `.` anywhere to bring up the shortcut completion window.

# Build and test

This section describes how you can build and test the extension with your local Visual Studio installation.
### Prerequisites
* Installation of Visual Studio ([Visual Studio 2022](https://visualstudio.microsoft.com/vs/) is recommended)
* MSBuild version `17.3.1`

MSBuild can be found in this folder for a standard installation:

 `C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin`
  
  You might want to add this folder to your PATH.
### Build the extension
To build the extension navigate to the project root and execute

`msbuild .\Extension.sln`
### Run unit tests

>_**Note: Does not work at the moment.**_

To run the unit tests execute

`dotnet test .\Extension.sln`
### Install the extension
Navigate to the project root and double click

`bin\Debug\Extension.vsix` .

This will install the extension to your local Visual Studio installation.

  

  
