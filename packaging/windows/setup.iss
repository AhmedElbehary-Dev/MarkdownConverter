[Setup]
AppId={{783eb077-d4fa-4dc4-bbf8-661cd9dc6ee8}
AppName=MarkdownConverter
AppVersion=2.1.0
AppPublisher=AhmedElbehary-Dev
AppPublisherURL=https://github.com/AhmedElbehary-Dev/MarkdownConverter
AppSupportURL=https://github.com/AhmedElbehary-Dev/MarkdownConverter/issues
AppUpdatesURL=https://github.com/AhmedElbehary-Dev/MarkdownConverter/releases
DefaultDirName={autopf}\MarkdownConverter
DefaultGroupName=MarkdownConverter
UninstallDisplayIcon={app}\MarkdownConverter.exe
SetupIconFile=..\..\img\md_converter.ico
Compression=lzma2
SolidCompression=yes
OutputDir=..\..\release
OutputBaseFilename=MarkdownConverter-Setup-x64
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=admin
CloseApplications=force
RestartApplications=no
SetupMutex=MarkdownConverterSetupMutex
; --- Code Signing (uncomment when you have a certificate) ---
; SignTool=signtool sign /tr http://timestamp.digicert.com /td sha256 /fd sha256 /a $f
; SignedUninstaller=yes

[Files]
Source: "..\..\bin\Release\net10.0\win-x64\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
; --- Bundled dependencies (from download-deps.ps1) ---
Source: "deps\wkhtmltox-installer.exe"; DestDir: "{tmp}"; Flags: deleteafterinstall ignoreversion
Source: "deps\libwkhtmltox.dll"; DestDir: "{app}\runtimes\win-x64\native"; Flags: ignoreversion
[Icons]
Name: "{group}\MarkdownConverter"; Filename: "{app}\MarkdownConverter.exe"
Name: "{autodesktop}\MarkdownConverter"; Filename: "{app}\MarkdownConverter.exe"; Tasks: desktopicon
Name: "{userstartup}\MarkdownConverter"; Filename: "{app}\MarkdownConverter.exe"; Parameters: "--minimized"; Tasks: startupicon

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "startupicon"; Description: "Start Markdown Converter with Windows (minimized to tray)"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Code]
var
  DependencyPage: TOutputProgressWizardPage;
  LogMemo: TNewMemo;
  PathsAddedToSystem: String;

procedure LogLine(Msg: String);
var
  S: String;
begin
  S := '[' + GetDateTimeString('hh:nn:ss', ':', ':') + '] ' + Msg;
  if LogMemo <> nil then
  begin
    LogMemo.Lines.Add(S);
    LogMemo.SelStart := Length(LogMemo.Text);
    LogMemo.SelLength := 0;
  end;
  Log(S);
end;

function IsOnSystemPath(Dir: String): Boolean;
var
  Path: String;
begin
  Result := False;
  if RegQueryStringValue(HKLM, 'SYSTEM\CurrentControlSet\Control\Session Manager\Environment', 'Path', Path) then
  begin
    Result := Pos(';' + Lowercase(Dir) + ';', ';' + Lowercase(Path) + ';') > 0;
  end;
end;

procedure AddToSystemPath(Dir: String);
var
  Path: String;
begin
  if RegQueryStringValue(HKLM, 'SYSTEM\CurrentControlSet\Control\Session Manager\Environment', 'Path', Path) then
  begin
    if Path <> '' then
    begin
      if Copy(Path, Length(Path), 1) <> ';' then
        Path := Path + ';';
    end;
    Path := Path + Dir;
    RegWriteStringValue(HKLM, 'SYSTEM\CurrentControlSet\Control\Session Manager\Environment', 'Path', Path);
    
    if PathsAddedToSystem <> '' then
      PathsAddedToSystem := PathsAddedToSystem + ';';
    PathsAddedToSystem := PathsAddedToSystem + Dir;
  end;
end;

function DetectBrowser(): String;
var
  Paths: array of String;
  I: Integer;
begin
  SetArrayLength(Paths, 4);
  Paths[0] := ExpandConstant('{pf}\Google\Chrome\Application\chrome.exe');
  Paths[1] := ExpandConstant('{pf32}\Google\Chrome\Application\chrome.exe');
  Paths[2] := ExpandConstant('{pf}\Microsoft\Edge\Application\msedge.exe');
  Paths[3] := ExpandConstant('{pf32}\Microsoft\Edge\Application\msedge.exe');
  
  Result := '';
  for I := 0 to 3 do
  begin
    if FileExists(Paths[I]) then
    begin
      Result := Paths[I];
      Exit;
    end;
  end;
end;

procedure RemoveFromSystemPath(Dir: String);
var
  Path: String;
  P: Integer;
begin
  if RegQueryStringValue(HKLM, 'SYSTEM\CurrentControlSet\Control\Session Manager\Environment', 'Path', Path) then
  begin
    Path := ';' + Path + ';';
    Dir := ';' + Dir + ';';
    P := Pos(Lowercase(Dir), Lowercase(Path));
    if P > 0 then
    begin
      Delete(Path, P, Length(Dir) - 1);
      Path := Copy(Path, 2, Length(Path) - 2);
      RegWriteStringValue(HKLM, 'SYSTEM\CurrentControlSet\Control\Session Manager\Environment', 'Path', Path);
    end;
  end;
end;

{ Kill any running MarkdownConverter processes to prevent file locks }
procedure KillRunningInstances();
var
  ResultCode: Integer;
begin
  Exec('taskkill.exe', '/F /IM MarkdownConverter.exe', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  { Small delay to ensure file locks are released }
  Sleep(500);
end;

{ PrepareToInstall runs BEFORE file extraction — this is the safe place
  to uninstall the old version without racing against new file writes. }
function PrepareToInstall(var NeedsRestart: Boolean): String;
var
  sUnInstPath: String;
  sUnInstallString: String;
  iResultCode: Integer;
begin
  Result := '';  { empty = no error, proceed with install }
  NeedsRestart := False;

  { Kill running instances first so the uninstaller and file extraction succeed }
  KillRunningInstances();

  { Check for existing installation }
  sUnInstPath := ExpandConstant('Software\Microsoft\Windows\CurrentVersion\Uninstall\{#emit SetupSetting("AppId")}_is1');
  sUnInstallString := '';
  if not RegQueryStringValue(HKLM, sUnInstPath, 'UninstallString', sUnInstallString) then
    RegQueryStringValue(HKCU, sUnInstPath, 'UninstallString', sUnInstallString);
  
  if sUnInstallString <> '' then
  begin
    sUnInstallString := RemoveQuotes(sUnInstallString);
    if FileExists(sUnInstallString) then
    begin
      Log('Running old uninstaller: ' + sUnInstallString);
      if not Exec(sUnInstallString, '/SILENT /NORESTART /SUPPRESSMSGBOXES', '', SW_HIDE, ewWaitUntilTerminated, iResultCode) then
        Log('Old uninstaller failed with code: ' + IntToStr(iResultCode));
    end
    else
    begin
      Log('Old uninstaller not found at: ' + sUnInstallString);
      { Clean orphan registry entry so Inno Setup does not create unins001 variant }
      RegDeleteKeyIncludingSubkeys(HKLM, sUnInstPath);
      RegDeleteKeyIncludingSubkeys(HKCU, sUnInstPath);
      Log('Cleaned orphan uninstall registry entry');
    end;
  end;

  { Ensure no stale unins* files remain that would cause Inno to create unins001 }
  if FileExists(ExpandConstant('{app}\unins000.exe')) then
    DeleteFile(ExpandConstant('{app}\unins000.exe'));
  if FileExists(ExpandConstant('{app}\unins000.dat')) then
    DeleteFile(ExpandConstant('{app}\unins000.dat'));
  if FileExists(ExpandConstant('{app}\unins001.exe')) then
    DeleteFile(ExpandConstant('{app}\unins001.exe'));
  if FileExists(ExpandConstant('{app}\unins001.dat')) then
    DeleteFile(ExpandConstant('{app}\unins001.dat'));
end;

procedure RunDependencySetup();
var
  BrowserPath, BrowserDir: String;
  WkhtmltopdfBin: String;
  ResultCode: Integer;
  WkhtmltopdfInstalledByUs: Integer;
begin
  DependencyPage.Show;
  try
    { Create the LogMemo AFTER Show so dimensions are valid }
    LogMemo := TNewMemo.Create(WizardForm);
    LogMemo.Parent := DependencyPage.Surface;
    LogMemo.Left := 0;
    LogMemo.Top := DependencyPage.ProgressBar.Top + DependencyPage.ProgressBar.Height + 12;
    LogMemo.Width := DependencyPage.SurfaceWidth;
    LogMemo.Height := DependencyPage.SurfaceHeight - LogMemo.Top;
    LogMemo.ReadOnly := True;
    LogMemo.ScrollBars := ssVertical;
    LogMemo.Font.Name := 'Consolas';
    LogMemo.Font.Size := 9;

    PathsAddedToSystem := '';
    WkhtmltopdfInstalledByUs := 0;

    { Step 1: DLL already copied by [Files] }
    DependencyPage.SetProgress(10, 100);
    DependencyPage.SetText('Verifying libwkhtmltox.dll...', '');
    Sleep(300);
    LogLine('[OK] libwkhtmltox.dll installed to runtimes directory');

    { Step 2: Detect browser }
    DependencyPage.SetProgress(25, 100);
    DependencyPage.SetText('Detecting installed browsers...', '');
    Sleep(300);
    BrowserPath := DetectBrowser();
    if BrowserPath <> '' then
    begin
      LogLine('[OK] Found browser: ' + BrowserPath);
      BrowserDir := ExtractFilePath(BrowserPath);
      if Length(BrowserDir) > 0 then
        BrowserDir := Copy(BrowserDir, 1, Length(BrowserDir) - 1);
      if not IsOnSystemPath(BrowserDir) then
      begin
        AddToSystemPath(BrowserDir);
        LogLine('[OK] Browser directory added to system PATH');
      end else
        LogLine('[OK] Browser already on system PATH');
    end else
    begin
      LogLine('[--] No Chrome/Edge found (PDF via browser will be unavailable)');
    end;

    { Step 3: Install wkhtmltopdf }
    DependencyPage.SetProgress(45, 100);
    DependencyPage.SetText('Checking wkhtmltopdf...', '');
    Sleep(300);
    WkhtmltopdfBin := ExpandConstant('{pf}\wkhtmltopdf\bin');
    if not FileExists(WkhtmltopdfBin + '\wkhtmltopdf.exe') then
    begin
      LogLine('[..] Installing wkhtmltopdf silently...');
      DependencyPage.SetText('Installing wkhtmltopdf (this may take a moment)...', '');
      if Exec(ExpandConstant('{tmp}\wkhtmltox-installer.exe'), '/VERYSILENT /NORESTART /SUPPRESSMSGBOXES', '', SW_HIDE, ewWaitUntilTerminated, ResultCode) then
      begin
        LogLine('[OK] wkhtmltopdf installed successfully');
        WkhtmltopdfInstalledByUs := 1;
      end else
      begin
        LogLine('[!!] wkhtmltopdf install returned code ' + IntToStr(ResultCode));
      end;
    end else
    begin
      LogLine('[OK] wkhtmltopdf already present - skipping');
    end;

    { Step 4: PATH for wkhtmltopdf }
    DependencyPage.SetProgress(75, 100);
    DependencyPage.SetText('Configuring system PATH...', '');
    Sleep(300);
    if FileExists(WkhtmltopdfBin + '\wkhtmltopdf.exe') then
    begin
      if not IsOnSystemPath(WkhtmltopdfBin) then
      begin
        AddToSystemPath(WkhtmltopdfBin);
        LogLine('[OK] wkhtmltopdf added to system PATH');
      end else
        LogLine('[OK] wkhtmltopdf already on system PATH');
    end;

    { Step 5: Write manifest }
    DependencyPage.SetProgress(90, 100);
    DependencyPage.SetText('Writing install manifest...', '');
    Sleep(200);
    LogLine('[..] Writing install manifest...');
    RegWriteDWordValue(HKLM, 'Software\MarkdownConverterTeam\MarkdownConverter\InstallManifest', 'WkhtmltopdfInstalled', WkhtmltopdfInstalledByUs);
    if WkhtmltopdfInstalledByUs = 1 then
      RegWriteStringValue(HKLM, 'Software\MarkdownConverterTeam\MarkdownConverter\InstallManifest', 'WkhtmltopdfUninstallCmd', ExpandConstant('{pf}\wkhtmltopdf\unins000.exe'));
    RegWriteStringValue(HKLM, 'Software\MarkdownConverterTeam\MarkdownConverter\InstallManifest', 'PathEntriesAdded', PathsAddedToSystem);
    RegWriteStringValue(HKLM, 'Software\MarkdownConverterTeam\MarkdownConverter\InstallManifest', 'InstallDate', GetDateTimeString('yyyy-mm-dd hh:nn:ss', '-', ':'));
    LogLine('[OK] Manifest saved');

    { Done }
    DependencyPage.SetProgress(100, 100);
    DependencyPage.SetText('Environment setup complete!', '');
    LogLine('------------------------------');
    LogLine('[OK] All PDF backends ready!');
    Sleep(1500);
  finally
    DependencyPage.Hide;
  end;
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if (CurStep = ssPostInstall) then
  begin
    RunDependencySetup();
  end;
end;

procedure InitializeWizard;
begin
  DependencyPage := CreateOutputProgressPage('Environment Setup', 'Setting up PDF conversion components...');
end;

procedure CurUninstallStepChanged(CurStep: TUninstallStep);
var
  WkhtmltopdfInstalled: Cardinal;
  WkhtmltopdfUninstallCmd, PathsAdded, PathItem: String;
  iResultCode, P: Integer;
begin
  if CurStep = usUninstall then
  begin
    { Kill running instances before uninstalling }
    KillRunningInstances();
  end
  else if CurStep = usPostUninstall then
  begin
    { Uninstall wkhtmltopdf only if WE installed it }
    if RegQueryDWordValue(HKLM, 'Software\MarkdownConverterTeam\MarkdownConverter\InstallManifest', 'WkhtmltopdfInstalled', WkhtmltopdfInstalled) then
    begin
      if (WkhtmltopdfInstalled = 1) and RegQueryStringValue(HKLM, 'Software\MarkdownConverterTeam\MarkdownConverter\InstallManifest', 'WkhtmltopdfUninstallCmd', WkhtmltopdfUninstallCmd) then
      begin
        if FileExists(WkhtmltopdfUninstallCmd) then
          Exec(WkhtmltopdfUninstallCmd, '/VERYSILENT /NORESTART /SUPPRESSMSGBOXES', '', SW_HIDE, ewWaitUntilTerminated, iResultCode);
      end;
    end;
    
    { Remove PATH entries we added }
    if RegQueryStringValue(HKLM, 'Software\MarkdownConverterTeam\MarkdownConverter\InstallManifest', 'PathEntriesAdded', PathsAdded) then
    begin
      while Length(PathsAdded) > 0 do
      begin
        P := Pos(';', PathsAdded);
        if P > 0 then
        begin
          PathItem := Copy(PathsAdded, 1, P - 1);
          Delete(PathsAdded, 1, P);
        end else
        begin
          PathItem := PathsAdded;
          PathsAdded := '';
        end;
        if PathItem <> '' then RemoveFromSystemPath(PathItem);
      end;
    end;
    
    { Clean our custom registry entries }
    RegDeleteKeyIncludingSubkeys(HKLM, 'Software\MarkdownConverterTeam\MarkdownConverter');
    RegDeleteKeyIncludingSubkeys(HKCU, 'Software\MarkdownConverterTeam');
    
    { Clean the start menu group - Inno Setup handles app dir cleanup itself }
    DelTree(ExpandConstant('{group}'), True, True, False);
  end;
end;

