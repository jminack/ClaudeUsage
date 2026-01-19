; Claude Usage Widget Installer
; NSIS Script

!include "MUI2.nsh"

; General
Name "Claude Usage Widget"
OutFile "..\ClaudeUsageWidget-Setup.exe"
InstallDir "$PROGRAMFILES64\ClaudeUsageWidget"
InstallDirRegKey HKLM "Software\ClaudeUsageWidget" "InstallDir"
RequestExecutionLevel admin

; Version info
!define VERSION "1.0.1"
VIProductVersion "1.0.1.0"
VIAddVersionKey "ProductName" "Claude Usage Widget"
VIAddVersionKey "ProductVersion" "${VERSION}"
VIAddVersionKey "FileDescription" "Claude Usage Widget Installer"
VIAddVersionKey "FileVersion" "${VERSION}"
VIAddVersionKey "LegalCopyright" "MIT License"

; Interface settings
!define MUI_ABORTWARNING
!define MUI_ICON "${NSISDIR}\Contrib\Graphics\Icons\modern-install.ico"
!define MUI_UNICON "${NSISDIR}\Contrib\Graphics\Icons\modern-uninstall.ico"

; Pages
!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_LICENSE "..\LICENSE"
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_COMPONENTS
!insertmacro MUI_PAGE_INSTFILES
!define MUI_FINISHPAGE_RUN "$INSTDIR\ClaudeUsageWidget.exe"
!define MUI_FINISHPAGE_RUN_TEXT "Launch Claude Usage Widget"
!insertmacro MUI_PAGE_FINISH

; Uninstaller pages
!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES

; Language
!insertmacro MUI_LANGUAGE "English"

; Installer sections
Section "Claude Usage Widget (required)" SecMain
    SectionIn RO

    ; Set output path
    SetOutPath "$INSTDIR"

    ; Install files
    File "..\publish\ClaudeUsageWidget.exe"

    ; Create uninstaller
    WriteUninstaller "$INSTDIR\Uninstall.exe"

    ; Registry keys for uninstall
    WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\ClaudeUsageWidget" "DisplayName" "Claude Usage Widget"
    WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\ClaudeUsageWidget" "UninstallString" '"$INSTDIR\Uninstall.exe"'
    WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\ClaudeUsageWidget" "DisplayVersion" "${VERSION}"
    WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\ClaudeUsageWidget" "Publisher" "Claude Usage Widget"
    WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\ClaudeUsageWidget" "NoModify" 1
    WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\ClaudeUsageWidget" "NoRepair" 1

    ; Store install dir
    WriteRegStr HKLM "Software\ClaudeUsageWidget" "InstallDir" "$INSTDIR"
SectionEnd

Section "Start Menu Shortcut" SecStartMenu
    CreateDirectory "$SMPROGRAMS\Claude Usage Widget"
    CreateShortcut "$SMPROGRAMS\Claude Usage Widget\Claude Usage Widget.lnk" "$INSTDIR\ClaudeUsageWidget.exe"
    CreateShortcut "$SMPROGRAMS\Claude Usage Widget\Uninstall.lnk" "$INSTDIR\Uninstall.exe"
SectionEnd

Section "Run at Windows Startup" SecStartup
    WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Run" "ClaudeUsageWidget" '"$INSTDIR\ClaudeUsageWidget.exe"'
SectionEnd

; Section descriptions
!insertmacro MUI_FUNCTION_DESCRIPTION_BEGIN
    !insertmacro MUI_DESCRIPTION_TEXT ${SecMain} "The main application files (required)."
    !insertmacro MUI_DESCRIPTION_TEXT ${SecStartMenu} "Create Start Menu shortcuts."
    !insertmacro MUI_DESCRIPTION_TEXT ${SecStartup} "Automatically start Claude Usage Widget when Windows starts."
!insertmacro MUI_FUNCTION_DESCRIPTION_END

; Uninstaller section
Section "Uninstall"
    ; Kill running instance
    nsExec::ExecToLog 'taskkill /F /IM ClaudeUsageWidget.exe'

    ; Remove files
    Delete "$INSTDIR\ClaudeUsageWidget.exe"
    Delete "$INSTDIR\Uninstall.exe"
    RMDir "$INSTDIR"

    ; Remove Start Menu shortcuts
    Delete "$SMPROGRAMS\Claude Usage Widget\Claude Usage Widget.lnk"
    Delete "$SMPROGRAMS\Claude Usage Widget\Uninstall.lnk"
    RMDir "$SMPROGRAMS\Claude Usage Widget"

    ; Remove startup entry
    DeleteRegValue HKCU "Software\Microsoft\Windows\CurrentVersion\Run" "ClaudeUsageWidget"

    ; Remove registry keys
    DeleteRegKey HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\ClaudeUsageWidget"
    DeleteRegKey HKLM "Software\ClaudeUsageWidget"
SectionEnd
