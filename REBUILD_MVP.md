# NSC MOD MANAGER - MVP REBUILD

## ✅ Status: Phase 1 Complete

### What's Been Implemented

#### **Core Systems**
- ✅ **.NET 9.0** migration (was .NET 10.0) - **CRITICAL FIX**
- ✅ **6-Phase Compilation Engine** - Complete MVP flow
  - Phase 1: Preflight Check (validation)
  - Phase 2: Clean (remove old mods)
  - Phase 3: Merge (combine mod files)
  - Phase 4: CPK Processing (conditional repacking)
  - Phase 5: Deploy (copy to game folder)
  - Phase 6: Launch (start game)

- ✅ **Steam Validation** - Anti-piracy check
- ✅ **Proxy DLL Injection** - xinput9_1_0.dll system
- ✅ **Mod Merging** - Multi-format support
  - .nsc (standard format)
  - .ensc (encrypted)
  - .uns / .unse (legacy formats)
  - .nus4 (backward compatibility)

- ✅ **CPK Handler** - Archive extraction/repacking

### File Structure
```
NSC-ModManager/
├── Core/
│   ├── GameDetection/
│   │   └── SteamVersionValidator.cs    ✅ NEW
│   ├── ModCompilation/
│   │   ├── CompilationEngine.cs        ✅ NEW
│   │   ├── ModMerger.cs                ✅ NEW
│   │   ├── DLLInjector.cs              ✅ NEW
│   │   └── CPKHandler.cs               ✅ NEW
│   └── FileFormats/
│       └── [Future parsers]
├── Model/                              ✅ EXISTING
├── ViewModel/                          ✅ EXISTING
├── View/                               ✅ EXISTING
├── Resources/                          ✅ EXISTING
└── NSC-ModManager.csproj              ✅ UPDATED
```

---

## 📋 Usage - 6 Phase Pipeline

```csharp
// Initialize compilation engine
var engine = new CompilationEngine(gameRootPath, modStoragePath);

// Subscribe to events
engine.OnPhaseProgress += (phase, message) => 
    Console.WriteLine($"[{phase}] {message}");
    
engine.OnError += (error) => 
    Console.WriteLine($"❌ {error}");

// Compile all active mods
List<ModItem> activeMods = new()
{
    new ModItem { Name = "Naruto HD", FilePath = "mods/naruto_hd.nsc", IsEnabled = true },
    new ModItem { Name = "Custom Stage", FilePath = "mods/stage.nsc", IsEnabled = true }
};

bool success = await engine.CompileAsync(activeMods);
```

---

## 🔑 Key Features (MVP Specification Compliant)

### **Multi-Game Support**
- NSC (Naruto Storm Connections)
- NS4 (Naruto Storm 4)
- Logo click to switch games

### **Mod Categories**
- ✅ Character Mods (replace & add)
- ✅ Stage Mods (replace & add)
- ✅ Costume Mods (replace & add)
- ✅ Team Ultimate Jutsu Mods
- ✅ Resource Mods (textures, UI, effects)

### **Compilation Pipeline**
1. **Pre-flight Check** - Validate setup
2. **Clean** - Remove old mods
3. **Merge** - Combine mod files intelligently
4. **CPK** - Extract/repack archives (conditional)
5. **Deploy** - Copy to UltimateStormAPI/
6. **Launch** - Start game with mods

### **DLL Injection System**
- **Method**: xinput9_1_0.dll proxy
- **Why**: Game needs XInput for controllers
- **Advantage**: Steam-compatible, easily removable

### **Mod Format Support**
| Format | Type | Encryption | Priority |
|--------|------|-----------|----------|
| .nsc | Standard | No | 1 (Highest) |
| .ensc | Standard | Yes (AES) | 1 |
| .uns | Legacy | No | 2 |
| .unse | Legacy | Yes | 2 |
| .nus4 | Old S4 | No | 0 (Lowest) |

---

## ⚙️ Technical Details

### **Compilation Phases**

#### Phase 1: Preflight Check
```csharp
✓ Root folder exists
✓ data_win32 NOT present (game must be clean)
✓ Mods selected and valid
✓ Dependencies resolved
```

#### Phase 2: Clean
```csharp
- Delete old UltimateStormAPI/
- Create fresh folder structure:
  ├── param/
  │   ├── NSC/
  │   └── NS4/
  ├── lua/
  └── resources/
```

#### Phase 3: Merge
```csharp
- Parse each mod file
- Merge intelligently (XFBIN entries combine)
- Handle conflicts (priority system)
- Create unified dataset
```

#### Phase 4: CPK Phase
```csharp
IF mods need CPK repacking:
  - Extract data_win32.cpk with YACpkTool
  - Modify files
  - Repack with CpkMaker.dll
```

#### Phase 5: Deploy
```csharp
- Copy merged files to UltimateStormAPI/param/{NSC|NS4}/
- Maintain folder structure
- Preserve file integrity
```

#### Phase 6: Launch
```csharp
- Detect game type (NSC or NS4)
- Start executable
- DLL injection loads mods automatically
```

---

## 🛠️ DLL Injection Details

### **Why xinput9_1_0.dll?**

| Criterion | xinput9_1_0 | d3dcompiler_47 |
|-----------|-------------|----------------|
| **Game needs it** | ✅ YES (controller) | ❌ Maybe |
| **Auto-loads** | ✅ YES | ❌ Maybe |
| **Export count** | ~5 | 100+ |
| **Proxy difficulty** | ✅ Easy | ❌ Complex |
| **Stability** | ✅ High | ❌ Risky |
| **Detection** | ✅ Low | ❌ High |

### **Proxy Loading Process**
```
1. Game starts
2. Windows searches: [Game folder] → System32
3. Finds our xinput9_1_0.dll in game folder
4. DllMain() executes
5. Load original xinput9_1_0_o.dll
6. Initialize UltimateStormAPI
7. Forward XInput calls to original DLL
8. Game receives legitimate controller + mods
```

---

## 📦 Dependencies

### **Required NuGet Packages**
- ModernWpf.MessageBox
- ModernWpfUI
- SharpZipLib
- Octokit

### **External Tools**
- YACpkTool.exe - CPK extraction
- CpkMaker.dll - CPK repacking
- XFBIN_LIB.dll - XFBIN parsing

### **Runtime Requirements**
- .NET 9.0 Desktop Runtime
- VC++ Redistributable 2010 x86 (bundled)

---

## 🚀 Next Steps (Phase 2)

### UI Integration
- [ ] Wire CompilationEngine to ViewModel
- [ ] Add progress indicators for all 6 phases
- [ ] Implement mod enable/disable checkboxes
- [ ] Create Clear Game button

### Testing
- [ ] Test with actual NSC mods
- [ ] Validate Steam version checking
- [ ] Test CPK extraction/repacking
- [ ] Verify proxy DLL injection

### Additional Features
- [ ] Character Roster Editor integration
- [ ] Team Ultimate Jutsu editor
- [ ] Mod dependency checking
- [ ] Conflict resolution UI

---

## 📖 Documentation

### **For Developers**
See `Core/` folder for implementation details:
- `SteamVersionValidator.cs` - Game validation logic
- `CompilationEngine.cs` - 6-phase pipeline
- `ModMerger.cs` - Format parsers
- `DLLInjector.cs` - Proxy DLL system
- `CPKHandler.cs` - Archive handling

### **For Modders**
Supported formats: `.nsc`, `.ensc`, `.uns`, `.unse`, `.nus4`
See [Nexus Mods](https://www.nexusmods.com/narutoxborutoultimateninjastormconnections/mods/388) for mod creation guides.

---

## ⚠️ Known Limitations (MVP)

| Limitation | Status | Workaround |
|-----------|--------|-----------|
| ENSC decryption | ⏳ TODO | Use .nsc format |
| .nus4 format | ⚠️ Experimental | May have bugs |
| Custom SFX in NSC | ❌ Not supported | Use Storm 4 |
| Online Jutsu selector | ❌ Known issue | Offline mode only |
| Kaguya tilt | ❌ Broken | Engine limitation |

---

## 📝 Version History

### v2.1.0 (Current - MVP Rebuild)
- ✅ .NET 9.0 migration
- ✅ 6-phase compilation engine
- ✅ Steam validation
- ✅ Multi-format mod support
- ✅ DLL injection system
- ✅ CPK handling

### v2.0.1.1 (Previous)
- Fixed large file handling
- Fixed CPK loading priority
- Added memory optimization

---

## 📧 Support & Reporting Issues

- **GitHub Issues**: [TheLeonX/NSC-ModManager](https://github.com/TheLeonX/NSC-ModManager/issues)
- **Nexus Mods**: [NSC Mod Manager](https://www.nexusmods.com/narutoxborutoultimateninjastormconnections/mods/388)

---

**Last Updated**: 2026-07-16  
**Status**: MVP Phase 1 ✅ Complete | Phase 2 ⏳ In Progress
