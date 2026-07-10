"""
DeadManZone - convert the remaining humanoid IronMarch units serially.
Robust: a failure on one unit is logged and skipped, never aborts the batch.

  python batch_convert.py            # convert the default remaining list
  python batch_convert.py u1 u2 ...  # convert an explicit list

Excludes non-humanoids (armored_transport, ironmarch_iron_horse,
machine_gun_nest) -- auto-rig only handles bipeds; those keep their old art.
"""

import subprocess
import sys
import time

# conscript_rifleman + the two pilots (shock_trooper, ironclad_field_marshal)
# are done separately; this is everything else humanoid.
REMAINING = [
    "enlisted_rifleman", "field_medic", "grenade_thrower", "ironclad_marksman",
    "ironclad_mortars", "ironmarch_breacher", "ironmarch_engineer",
    "ironmarch_sniper", "ironmarch_surgeon", "bulwark_squad",
    "marksman_squad", "rifle_squad",
]


def main():
    units = sys.argv[1:] or REMAINING
    results = {}
    for i, unit in enumerate(units, 1):
        print(f"\n{'='*60}\n[{i}/{len(units)}] {unit}\n{'='*60}", flush=True)
        t0 = time.time()
        try:
            subprocess.run([sys.executable, "convert_unit.py", unit],
                           check=True)
            results[unit] = f"OK ({time.time()-t0:.0f}s)"
        except subprocess.CalledProcessError as e:
            results[unit] = f"FAILED (exit {e.returncode})"
        print(f"--> {unit}: {results[unit]}", flush=True)

    print(f"\n{'='*60}\nBATCH SUMMARY\n{'='*60}")
    for unit, status in results.items():
        print(f"  {unit:26} {status}")
    ok = sum(1 for s in results.values() if s.startswith("OK"))
    print(f"\n{ok}/{len(units)} succeeded")


if __name__ == "__main__":
    main()
