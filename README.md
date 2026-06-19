# Fast Track

> Privacy-first intermittent fasting tracker. Local-only by design. No accounts, no servers, no analytics — your data stays on your phone.

Fast Track is a .NET MAUI 9 app for iOS and Android that times your fasts, walks you through the seven metabolic stages your body moves through, and keeps a quiet, encouraging log of what you've done. It's built for people who want to *understand* fasting, not gamify it into anxiety.

---

## What it does

- **Time the fast.** A focused circular timer with the elapsed hours and a goal arc. Edit the start time after the fact when you forgot to hit Start.
- **Seven-stage roadmap.** As you cross 4h → 12h → 18h → 24h → 48h → 72h, the active stage card surfaces what's happening biologically (anabolic → catabolic → fat-burning → ketosis → autophagy → deep ketosis → extended). Tap any stage card for a deeper read.
- **Daily health card.** Optional water/mood/weight logging with animated number tweens. Quick-add chips for `+250 / +500 / +1 L` water.
- **Quests, streaks, levels, badges.** Gentle gamification — daily quests, a streak that respects freeze tokens, an XP/level ladder, and a Trophy Cabinet. Celebration page when you hit a goal, with confetti and per-card pop-ins.
- **Protocols.** 16:8, 18:6, 20:4, OMAD, 5:2, and a custom-protocol builder. Each with its own SVG icon and explanation.
- **History.** Every completed fast, with swipe-to-delete and a long-press menu. Pull-to-refresh with haptic feedback.
- **Insights.** Weekly summary, fasting heatmap, range selector (7d / 30d / 90d / 1y / All), fast-duration distribution, weekly hours bar chart, weight trend, water chart, and a "Personal Bests" panel.
- **Learn tab.** Seven hand-written articles ("What is IF?", "Breaking a fast safely", "Hunger waves and ghrelin", "Electrolytes", "When to stop") plus a stage glossary that deep-links to the existing detail modals.
- **Customize Home.** Toggle which cards appear on your dashboard — gamification strip, daily health, quests, goal/progress cards, stages roadmap. Auto-saves on every flip.
- **Privacy & Terms.** Honest copy explaining that there's no server in the loop, no analytics SDK, no advertising identifier. Plus a medical disclaimer named after the at-risk groups it's meant to protect.
- **Local notifications.** Stage-milestone reminders with quiet-hours support.

## What it doesn't do

- Sync to a cloud.
- Talk to a server.
- Run any analytics, telemetry, or crash reporting.
- Diagnose, treat, or prevent any medical condition. Fast Track is a logging tool. If you're pregnant, under 18, managing diabetes, on medication that requires food, or have a history of disordered eating, please talk to a healthcare professional before starting.

---

## Tech stack

- **.NET MAUI 9** — iOS, Android, MacCatalyst, Windows. (Currently shipping Android first; iOS uses the same Core library.)
- **`FastTrack.Core`** — net9.0 class library holding every ViewModel, model, service interface, and the in-memory catalogs. Knows nothing about Microsoft.Maui.Controls, so it's unit-testable without spinning up a MAUI host.
- **`FastTrack.Tests`** — xUnit + Moq + FluentAssertions. ~375+ tests covering ViewModels, services, calculators, and data round-trips.
- **CommunityToolkit.Mvvm** — `[ObservableProperty]`, `[RelayCommand]`, `[NotifyPropertyChangedFor]` everywhere.
- **sqlite-net-pcl + SQLitePCLRaw.bundle_green** — encrypted SQLite database in the app's private sandbox.
- **SkiaSharp + Microcharts.Maui** — custom GraphicsView for the active-fast ring and confetti, Microcharts for Insights.
- **Plugin.LocalNotification** — stage-milestone reminders, no remote push.
- **Inter + Manrope variable fonts** — typography system with weight axis controlled via `FontAttributes`.

## Repository layout

```
FastTrack/
├─ FastTrack.csproj                  ← MAUI head project (Android/iOS/MacCat/Windows)
├─ MainPage.xaml(.cs)                ← Home tab
├─ AppShell.xaml(.cs)                ← Shell + route registration
├─ MauiProgram.cs                    ← Dependency injection
│
├─ FastTrack.Core/                   ← net9.0 class library — all the logic
│  ├─ Models/                        ← FastingStage, UserProfile, Article, …
│  ├─ Services/Interfaces/           ← IFastingService, IStageCalculator, …
│  ├─ Services/Implementations/      ← FastingService, StageCalculator, …
│  ├─ Data/                          ← Repository interfaces + EF-free SQLite impls
│  └─ ViewModels/                    ← HomeViewModel, LearnViewModel, …
│
├─ FastTrack.Tests/                  ← xUnit suite
│
├─ Views/                            ← XAML pages + code-behind
│  ├─ BootPage                       ← Cold-launch traffic controller
│  ├─ OnboardingPage                 ← First-run wizard
│  ├─ HistoryPage / ProtocolsPage / InsightsPage / LearnPage / SettingsPage
│  ├─ PrivacyPage                    ← Privacy & Terms
│  ├─ CustomizeHomePage              ← Dashboard widget toggles
│  └─ … detail pages
│
├─ Helpers/                          ← AnimatedNumberLabel, ConfettiDrawable, LongPress
├─ Services/Implementations/         ← MAUI-specific service impls (Haptic, Dialog, Nav)
├─ Resources/
│  ├─ AppIcon/                       ← appicon.svg + appiconfg.svg + appiconfg_mono.svg
│  ├─ Splash/                        ← splash.svg
│  ├─ Fonts/                         ← Inter, Manrope variable TTFs
│  ├─ Images/                        ← Article hero SVGs, stage SVGs, tab icons, etc.
│  └─ Styles/                        ← Colors.xaml + FastTrack.xaml typography + Styles.xaml
│
├─ Platforms/Android/                ← Manifest, MainActivity, ProGuard rules
│
└─ docs/
   ├─ Release.md                     ← Keystore + signed-AAB workflow
   └─ PrivacyPolicy.md               ← Hosted-version of in-app Privacy & Terms
```

---

## Getting started

### Prerequisites

- **.NET 9 SDK** (`dotnet --version` ≥ 9.0).
- **MAUI workloads:** `dotnet workload install maui`.
- **Android side:** Android SDK 35, build-tools 35.0.0+, an emulator or a USB-debugging-enabled device.
- **iOS side:** Xcode 15+, an Apple developer account, and a Mac.

### Clone + run

```bash
git clone https://github.com/KirkPatrickJunsay/fast-track.git
cd fast-track
dotnet restore FastTrack.csproj

# Run on a connected Android device (find your serial via `adb devices`)
dotnet build FastTrack.csproj -f net9.0-android -t:Install -p:AdbTarget=-s<deviceSerial>
adb -s <deviceSerial> shell am start -n com.codesandchips.fasttrack/crc64bdb2837005ae90d8.MainActivity
```

### Run the tests

```bash
dotnet test FastTrack.Tests/FastTrack.Tests.csproj
```

Roughly half a second from clean. The suite covers every ViewModel, every catalog, and a chunky portion of the service layer.

### Production build (signed AAB for Play Store)

See [`docs/Release.md`](docs/Release.md) for the keystore generation, `signing.local.props` template, and the full publish workflow. Short version:

```bash
dotnet publish FastTrack.csproj -f net9.0-android -c Release
# → bin/Release/net9.0-android/publish/com.codesandchips.fasttrack-Signed.aab
```

The Release configuration switches to AAB output, enables profiled AOT with LLVM, runs R8 with our top-up rules at `Platforms/Android/proguard.cfg`, and signs with the keystore creds from a gitignored `signing.local.props`.

---

## Architecture notes

### Why `FastTrack.Core` exists

ViewModels and business logic live in a plain `net9.0` class library so the test project doesn't need to spin up a MAUI host (which doesn't run headless on macOS). The main `FastTrack.csproj` references `FastTrack.Core` via `<ProjectReference>` and explicitly excludes the Core folder from its own compile graph. This split is what makes 375+ fast unit tests possible.

### Cold-launch routing

A dedicated `BootPage` is the first `ShellContent`. It visually matches the system splash (`#131313` + ring SVG), reads `UserProfile.OnboardingCompleted` in `OnAppearing`, and routes to either `//MainPage` or `//OnboardingPage` before either is drawn. No first-run Home flash, no returning-user Onboarding flash.

### Dependency injection

Everything is registered in `MauiProgram.cs`:

- **Singletons:** repositories, services, calculators, orchestrators.
- **Transients:** every page + every ViewModel — fresh instances per navigation so query attributes don't bleed across visits.

Pages take their ViewModel as a ctor parameter; MAUI's `DataTemplate` factory resolves the page through the DI container.

### Privacy posture

All seven repositories write to a single SQLite database at `Path.Combine(FileSystem.AppDataDirectory, "fasttrack.db3")` — Android's private app-data sandbox, OS-isolated from other apps. No outbound HTTP. No analytics SDK in `MauiProgram` (intentional). The medical disclaimer and "no servers" claim on the Privacy & Terms page are both load-bearing legal claims and architecturally true.

---

## License

[MIT](LICENSE). Use the code however you like; the brand "Fast Track" and the Codes & Chips marks aren't covered by the MIT grant — just the source.

## Acknowledgements

Inter and Manrope are licensed under the SIL Open Font License. SkiaSharp, Microcharts.Maui, Plugin.LocalNotification, sqlite-net-pcl, and CommunityToolkit.Mvvm are open-source projects without which this app would have been three times the work.
