# Adeptik NodeJs Redistributable

MSBuild targets for:
* NodeJs installation
* Yarn installation
* Node modules installation via yarn
* Run node scripts from package.json via yarn

Supports Windows, Linux & MacOS

![Build Status](https://tfs.adeptik.com/Adeptik/_apis/public/build/definitions/5f6da651-409b-4516-b0c6-16518d60e6e9/137/badge)
[![Nuget Package](https://img.shields.io/nuget/vpre/Adeptik.NodeJs.Redistributable)](https://www.nuget.org/packages/Adeptik.NodeJs.Redistributable/)

## Usage

### Package installation

Install Adeptik.NodeJs.Redistributable package from Nuget

    Install-Package Adeptik.NodeJs.Redistributable -Version 2.0.2

or add in your .csproj:

    <PackageReference Include="Adeptik.NodeJs.Redistributable" Version="2.0.2" />

### NodeJs installation

Specify a version of NodeJs you wish to use by adding property in .csproj:

```xml
<PropertyGroup>
    <NodeJsVersion>12.15.0</NodeJsVersion>
</PropertyGroup>
```

Now you can get downloaded distribution of NodeJs in your target by setting target's `DependsOn` attribute to `InstallNodeJs` and reading `$(NodeJsPath)`, `$(GlobalNodeModulesPath)`, `$(NodeExecutable)` and `$(NPMExecutable)` properties.

```xml
<Target Name="YourTarget" DependsOnTargets="InstallNodeJs">
    <Message Text="Using $(NodeJsPath)" />
</Target>
```
- `$(NodeJsPath)` contains full path to downloaded & unpacked NodeJs distrib
- `$(GlobalNodeModulesPath)` contains full path to node_modules directory where global packages are stored
- `$(NodeExecutable)` contains command (with full path) to run node executable
- `$(NPMExecutable)` contains command (with full path) to run npm

### Yarn installation

You can install yarn package manager in your target by setting target's `DependsOn` attribute to `InstallYarn`. InstallYarn target depends on InstallNodeJs target. 

You can specify yarn version by setting `$(YarnVersion)` property. By default `$(YarnVersion)` set to `latest`. 

```xml
<PropertyGroup>
    <NodeJsVersion>12.15.0</NodeJsVersion>
    <YarnVersion>1.22.0</YarnVersion>
</PropertyGroup>
```
Yarn command line stored to `$(YarnExecutable)` property.

### Node modules installation via Yarn

You can install node modules (listed in package.json file in the root of your project) using yarn package manager in your target by setting target's `DependsOn` attribute to `YarnInstall`. YarnInstall target depends on InstallYarn target.

### Run node scripts from package.json via Yarn

You can run node scripts described in package.json using Yarn by setting `$(YarnBuildCommand)`. Target `YarnRun` automatically runs this command before `CoreCompile`. Also note that YarnRun target depends on YarnInstall target. So you can set only two properties to get node, yarn, install node modules and run script, like shown below.

```xml
<PropertyGroup>
    <NodeJsVersion>12.15.0</NodeJsVersion>
    <YarnVersion>1.22.0</YarnVersion>
    <YarnBuildCommand>build</YarnBuildCommand>
</PropertyGroup>
```
