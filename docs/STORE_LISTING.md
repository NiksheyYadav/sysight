# Microsoft Store listing: Sysight

Paste these into Partner Center → your app → Submission → Store listings → English.

## Product name
Sysight

## Short description (max 100 chars, used on some surfaces)
See every process, service, kernel driver and startup app on your PC, and take back disk space.

## Description
Sysight shows you everything running on your Windows PC in one place, and lets you act on it safely.

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
• Clear caches that apps rebuild by themselves: temp files, browser caches, npm, pip, Windows Update downloads and the Recycle Bin

**Built to be safe**
• Core Windows processes and Windows services are protected and can't be ended or disabled from Sysight
• Every service and startup change is recorded in Change history with a one-click Undo
• Kernel drivers are view-only
• Cleanup never touches your documents, projects or passwords
• Sysight collects no data and sends nothing over the internet

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
Safe cache cleanup
Change history with one-click Undo
Protection for core Windows processes and services
No telemetry, works offline

## Search terms (max 7)
task manager, system monitor, startup manager, disk cleanup, services, drivers, pc cleaner

## Category
Utilities & tools → (subcategory) System utilities / Security if offered

## Screenshots (1366×768, in this order)
docs/screenshots/01-overview.png  – "Your whole PC at a glance"
docs/screenshots/02-procs.png     – "Every process, who made it, what it uses"
docs/screenshots/03-drivers.png   – "See which drivers run in the kernel"
docs/screenshots/04-services.png  – "Tame background services"
docs/screenshots/05-startup.png   – "Choose what starts with Windows"
docs/screenshots/06-storage.png   – "Find what's filling your drive"
docs/screenshots/07-clean.png     – "Clear caches safely"
docs/screenshots/08-changes.png   – "Undo any change"
All screenshots use built-in sample data (no real machine names or user paths).

## Store logo
docs/store-logo-300.png (300×300). Partner Center also accepts the 1:1 box art at 1080×1080 if you want to add it later.

## Privacy policy URL
Required because the app requests restricted capabilities. Host docs/privacy.html (GitHub Pages works) and paste its URL.

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
> Sysight protects core Windows processes and Windows services from being ended or disabled, logs every change with a one-click Undo, and collects no data.
