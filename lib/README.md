# Local libraries

Projects and packages referenced by the solution from the repo instead of NuGet.

## NSubstitute 6.0.0

The solution uses NSubstitute 6.0.0 as a **local library** (not from NuGet/GitHub Packages).

- **`lib/NSubstitute/`** – wrapper project that references the NSubstitute DLLs.
- **`lib/NSubstitute.6.0.0/`** – package contents. Populate before building:

  1. From the repo root, run:  
     `.\scripts\Copy-NSubstituteFromCache.ps1`
  2. Commit `lib/NSubstitute.6.0.0/` so CI can build.

If that folder is empty, the NSubstitute project (and any test projects that use it) will fail to build. See `lib/NSubstitute.6.0.0/README.md` for manual copy instructions.
