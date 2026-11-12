# Microsoft Store listing: Sysight

Paste these into Partner Center → your app → Submission → Store listings → English.

## Product name
Sysight

ASO tip: the name is the strongest Microsoft Store ranking signal. In Partner Center → Product management → Manage app names, reserve a second name **"Sysight: Task Manager & Startup Manager"** (39 chars) and pick it in the Store listing. It keeps the brand first and adds the two highest-intent terms.

## Short description (max 100 chars, used on some surfaces)
Task manager, startup manager and disk cleaner for Windows, with kernel driver viewer and undo.

## Description
Sysight is a task manager, startup manager and disk cleaner for Windows 10 and 11. See every process, service, kernel driver and startup app on your PC, find what is filling your drive, and undo the service and startup changes you make. It works offline and collects no data.

It replaces several system tools: Task Manager, the Services console, startup checkers, a disk space analyzer and a PC cleaner.

**See everything**
• Live CPU, memory and drive usage
• Every running process with its publisher, CPU use, memory and file location
• Every network connection and listening port, with the app that owns it
• All background services, with who made them and which account they run as
• All kernel drivers, with their signer, and flags for drivers that can access hardware directly, see all network traffic, or watch all file access
• Every app that starts when you sign in
• Every installed app and how much space it takes

**Take control**
• End a misbehaving process
• Start, stop, set to Manual, or disable third-party services
• Turn startup apps on or off
• Launch an app's own uninstaller
• Drill into any folder by size to find what is filling your drive
• Clear caches that apps rebuild by themselves: temp files, browser caches, npm, pip, Windows Update downloads and the Recycle Bin (files are deleted permanently)

**Built with safeguards**
• Key Windows processes and Microsoft services are blocked from being ended or disabled
• Service and startup changes are recorded in Change history with one-click Undo (ending a process and cleanup can't be undone)
• Kernel drivers are view-only
• Cleanup only targets cache and temp folders, not your documents or saved passwords
• Sysight itself collects no data and makes no network requests

## What's new in this version
First release.

## Product features (up to 20, one per line)
Live CPU, memory and drive monitor
Process list with publisher and file path
Network connections with owning app
Service manager for third-party services
Kernel driver inspector with signer and risk flags
Startup app manager
Installed apps with sizes
Folder-by-size storage explorer
Cache cleanup
Change history with one-click Undo
Blocks changes to key Windows processes and services
No telemetry, works offline

## Search terms (max 7)
task manager, startup manager, pc cleaner, disk space analyzer, service manager, driver viewer, system monitor
(7 terms, each under 30 chars. Don't add competitor product names; the Store treats that as misleading metadata.)

## Category
Utilities & tools → (subcategory) System utilities / Security if offered

## Screenshots (1366×768, in this order)
docs/screenshots/01-overview.png  – "Task manager for Windows: CPU, memory and drives at a glance"
docs/screenshots/02-procs.png     – "Process manager: every process, its publisher and what it uses"
docs/screenshots/03-drivers.png   – "Kernel driver viewer with signer and risk flags"
docs/screenshots/04-services.png  – "Service manager: stop or disable background services"
docs/screenshots/05-startup.png   – "Startup manager: choose what starts with Windows"
docs/screenshots/06-storage.png   – "Disk space analyzer: find what fills your drive"
docs/screenshots/07-clean.png     – "PC cleaner for temp files and app caches"
docs/screenshots/08-changes.png   – "Undo service and startup changes in one click"
All screenshots use built-in sample data (no real machine names or user paths).

## Website
https://niksheyyadav.github.io/sysight/ (paste into Properties → Website)

## Store logo
docs/store-logo-300.png (300×300). Partner Center also accepts the 1:1 box art at 1080×1080 if you want to add it later.

## Privacy policy URL
https://niksheyyadav.github.io/sysight/privacy.html

## Additional license terms URL (Properties, optional)
https://niksheyyadav.github.io/sysight/terms.html

## Copyright and trademark info
© 2026 astrosurfr. Microsoft, Windows and Microsoft Edge are trademarks of the Microsoft group of companies. Sysight is not affiliated with Microsoft.

## Pricing
Suggested: Free at launch to collect ratings, or a one-time ₹199 / US$2.99. Don't promise features that aren't built yet.

## Age rating questionnaire
No user-generated content, no chat, no purchases, no location, no personal data collection → expect a 3+ rating.

## Restricted capabilities: justification text
Paste into the "Submission options → Restricted capabilities" box:

> Sysight is a system utility that shows and manages processes, services, startup apps and caches.
> • runFullTrust: it's a desktop (Win32) app that reads process, service and driver information through standard Windows APIs (WMI, Service Control Manager, registry).
> • allowElevation: service start/stop/disable needs administrator rights. Sysight runs as a standard user and only asks for elevation when the user clicks "Restart as admin".
> • unvirtualizedResources: turning startup apps on or off writes the same HKCU StartupApproved values Task Manager uses, and cache cleanup deletes files in the user's real AppData. With virtualization on, these changes would only affect a private copy and do nothing.
> Sysight blocks ending key Windows processes and changing Microsoft services, logs service and startup changes with one-click Undo, and collects no data.
