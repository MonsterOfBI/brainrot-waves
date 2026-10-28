"""Генерирует описания дублей (TrailerShot JSON) для всех языков и форматов.

python -I Trailer/tools/make_shots.py            -> Trailer/shots/*.json
Имя файла: <дубль>_<lang>_<fmt>.json для дублей с интерфейсом, <дубль>_<fmt>.json для пролётов без текста.
fmt: h = 1920x1080, v = 1080x1920.
"""
import copy
import json
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, "..", "shots")

T = {
    "wave": {"EN": "Wave {0} / {1}", "RU": "Волна {0} / {1}"},
    "wave_endless": {"EN": "Wave {0}", "RU": "Волна {0}"},
    "left": {"EN": "Enemies left: {n} · {arena}", "RU": "Осталось врагов: {n} · {arena}"},
    "left_survival": {"EN": "Enemies left: {n} · Survival · Cappuccino City", "RU": "Осталось врагов: {n} · Выживание · Город Капучино"},
    "arena_banner": {"EN": "ARENA 1 / 5", "RU": "АРЕНА 1 / 5"},
    "arena_banner_sub": {"EN": "Tralalero Beach · owner: Tralalero Tralala", "RU": "Пляж Тралалеро · хозяин: Тралалеро Тралала"},
    "wave_banner": {"EN": "WAVE {0} / {1}", "RU": "ВОЛНА {0} / {1}"},
    "friends": {"EN": "{0} and friends are coming!", "RU": "{0} и компания идут!"},
    "survival": {"EN": "SURVIVAL", "RU": "ВЫЖИВАНИЕ"},
    "survival_sub": {"EN": "How many waves can you last?", "RU": "Сколько волн ты продержишься?"},
    "defense": {"EN": "DEFEND THE PIZZERIA", "RU": "ЗАЩИТА ПИЦЦЕРИИ"},
    "defense_sub": {"EN": "Don't let the brainrots wreck Nonna's oven!", "RU": "Не дай брейнротам разрушить печь Нонны!"},
}
NAMES = {
    "Chimpanzini": {"EN": "Chimpanzini", "RU": "Шимпанзини"},
    "Patapim": {"EN": "Patapim", "RU": "Патапим"},
}


def tr(key, lang, *args):
    return T[key][lang].format(*args) if args else T[key][lang]


def hud_shots(lang):
    """Дубли с интерфейсом — пишутся на каждом языке."""
    return [
        dict(id="rifle_beach", hero="TungTung", arenaOwner="Tralalero", weapon="Rifle",
             status=tr("wave", lang, 3, 5), statusSub=tr("left", lang), waveNumber=3,
             bannerTitle=tr("arena_banner", lang), bannerSubtitle=tr("arena_banner_sub", lang), bannerAt=0.2,
             enemies=["Tralalero", "TrippiTroppi", "Trulimero", "Burbaloni", "Blueberrini"],
             initialCount=12, spawnEvery=0.45, spawnPack=2, maxAlive=24, distMin=13, distMax=28, arc=80,
             strafe=1.5, warmup=1.0, duration=6.0, seed=3),
        dict(id="shotgun_jungle", hero="Cappuccino", arenaOwner="Chimpanzini", weapon="Shotgun",
             status=tr("wave", lang, 4, 5), statusSub=tr("left", lang), waveNumber=4,
             enemies=["Chimpanzini", "Boneca", "Glorbo", "Orangutini", "Cocofanto"],
             initialCount=10, spawnEvery=0.5, spawnPack=2, maxAlive=20, distMin=9, distMax=20, arc=70,
             keepAway=4.5, damage=1.8, strafe=1.0, warmup=1.0, duration=5.0, seed=11),
        dict(id="ability_bomb", hero="Bombardiro", arenaOwner="Bombardiro", weapon="Rifle",
             status=tr("wave", lang, 5, 5), statusSub=tr("left", lang), waveNumber=5,
             enemies=["Lirili", "FrigoCamelo", "CactoHippo", "Svinino"],
             initialCount=16, spawnEvery=0.4, spawnPack=2, maxAlive=28, distMin=12, distMax=26, arc=60,
             abilities=["Q@0.6", "E@3.2"], damage=1.6, warmup=1.0, duration=5.5, seed=5),
        dict(id="ability_vortex", hero="Tralalero", arenaOwner="Patapim", weapon="Rifle",
             status=tr("wave", lang, 2, 5), statusSub=tr("left", lang), waveNumber=2,
             enemies=["Patapim", "Bombombini", "Glorbo", "Burbaloni"],
             initialCount=14, spawnEvery=0.5, spawnPack=2, maxAlive=24, distMin=10, distMax=22, arc=70,
             abilities=["E@0.5", "Q@3.0"], damage=1.5, warmup=1.0, duration=5.0, seed=8),
        dict(id="boss_intro", hero="Tralalero", arenaOwner="Bombardiro", weapon="Rifle",
             boss="Bombardiro", bossHealth=6000, enemies=[], initialCount=0,
             shoot=False, warmup=0.3, duration=4.3, seed=2),
        dict(id="boss_fight", hero="Tralalero", arenaOwner="Bombardiro", weapon="Rifle",
             boss="Bombardiro", bossHealth=6000, rageAt=2.2, preferBoss=True,
             enemies=["Lirili", "Svinino"], initialCount=3, spawnEvery=1.6, spawnPack=1, maxAlive=8,
             distMin=12, distMax=20, arc=60, damage=2.5, warmup=4.6, duration=6.0, seed=2),
        dict(id="survival_city", mode="Survival", hero="Ballerina", arenaOwner="Cappuccino", weapon="Rifle",
             status=tr("wave_endless", lang, 15), statusSub=tr("left_survival", lang), waveNumber=15,
             bannerTitle=tr("survival", lang), bannerSubtitle=tr("survival_sub", lang), bannerAt=0.2,
             enemies=["Bobritto", "ChefCrab", "RhinoToaster", "Cappuccino", "TaTaSahur", "Girafa", "Trulimero"],
             initialCount=18, spawnEvery=0.35, spawnPack=2, maxAlive=32, distMin=12, distMax=28, arc=90,
             damage=1.8, strafe=1.5, warmup=1.0, duration=6.0, seed=21),
        dict(id="defense_pizzeria", mode="Defense", hero="Chimpanzini", arenaOwner="Patapim", weapon="Rifle",
             status=tr("wave", lang, 7, 15), statusSub=tr("left", lang), waveNumber=7,
             bannerTitle=tr("defense", lang), bannerSubtitle=tr("defense_sub", lang), bannerAt=0.2,
             enemies=["Patapim", "Glorbo", "Bombombini", "Boneca", "Lirili"],
             initialCount=12, spawnEvery=0.5, spawnPack=2, maxAlive=24, distMin=9, distMax=20, arc=110,
             spawnCenter=[0, 0, 4], playerPos=[-5, 0.1, -10], playerYaw=22, keepAway=4,
             damage=1.6, warmup=1.0, duration=6.0, seed=13),
        dict(id="menu_roster", scene="MainMenu", menuMode="Survival", menuEvery=0.17, warmup=1.2, duration=4.6),
    ]


FLY = [
    ("beach", "Tralalero", ["Tralalero", "TrippiTroppi", "Trulimero", "Burbaloni", "Blueberrini"]),
    ("forest", "Patapim", ["Patapim", "Bombombini", "Glorbo", "Boneca"]),
    ("jungle", "Chimpanzini", ["Chimpanzini", "Orangutini", "Cocofanto", "Glorbo"]),
    ("desert", "Bombardiro", ["Lirili", "FrigoCamelo", "CactoHippo", "Svinino", "Bombardiro"]),
    ("city", "Cappuccino", ["Cappuccino", "Ballerina", "Bobritto", "ChefCrab", "RhinoToaster"]),
    ("night", "TungTung", ["TungTung", "TaTaSahur", "VacaSaturno", "Girafa"]),
]


def cine_shots():
    """Пролёты без интерфейса и текста — одинаковые для обоих языков."""
    shots = [
        dict(id="hook_tung", hero="Tralalero", arenaOwner="TungTung", weapon="Shotgun", hud=False,
             boss="TungTung", bossDirect=True, bossPos=[0, 0, 9], bossHealth=999999, preferBoss=True,
             enemies=["TungTung", "TaTaSahur"], initialCount=4, spawnEvery=0.8, spawnPack=1, maxAlive=7,
             distMin=8, distMax=14, arc=50, keepAway=2.5, damage=0.6, aimSmooth=0.14,
             warmup=0.4, duration=4.5, seed=4),
        dict(id="cine_orbit_tung", hero="Tralalero", arenaOwner="TungTung", hud=False, camera="orbit",
             boss="TungTung", bossDirect=True, bossPos=[0, 0, 8], bossHealth=999999, lookAtBoss=True,
             orbitRadius=15, orbitHeight=4.5, orbitDegrees=60, orbitStart=-25, camFov=48,
             enemies=["TaTaSahur", "Girafa"], initialCount=6, spawnEvery=0.8, spawnPack=1, maxAlive=8,
             spawnCenter=[0, 0, 8], distMin=3, distMax=8, arc=360, killEvery=0.8, shoot=False,
             warmup=0.6, duration=5.0, seed=9),
    ]
    for name, owner, enemies in FLY:
        shots.append(dict(id="cine_fly_" + name, hero="Tralalero" if owner != "Tralalero" else "TungTung",
                          arenaOwner=owner, hud=False, camera="dolly",
                          camFrom=[-16, 10, -22], camTo=[7, 5, -10], lookAt=[0, 1, 6], camFov=55,
                          enemies=enemies, initialCount=24, spawnEvery=0.3, spawnPack=2, maxAlive=34,
                          spawnCenter=[0, 0, 12], distMin=3, distMax=16, arc=360, killEvery=0.3, shoot=False,
                          warmup=0.8, duration=3.5, seed=17))
    return shots


def finalize(s, lang, fmt):
    s = copy.deepcopy(s)
    s.setdefault("scene", "Game")
    s.setdefault("mode", "Story")
    s["lang"] = lang
    s["fps"] = 60
    if fmt == "v":
        s["width"], s["height"] = 1080, 1920
        if s.get("camera", "fps") != "fps":
            s["camFov"] = round(s.get("camFov", 55) * 1.35, 1)
            if "orbitRadius" in s:
                s["orbitRadius"] = s["orbitRadius"] * 1.15
    else:
        s["width"], s["height"] = 1920, 1080
    for k in ("spawnCenter", "playerPos", "camFrom", "camTo", "lookAt", "bossPos"):
        if k in s:
            x, y, z = s[k]
            s[k] = {"x": x, "y": y, "z": z}
    s["outDir"] = "Logs/Trailer/out"
    return s


def main():
    os.makedirs(OUT, exist_ok=True)
    written = []
    for fmt in ("h", "v"):
        for lang in ("EN", "RU"):
            for s in hud_shots(lang):
                f = finalize(s, lang, fmt)
                f["id"] = "{}_{}_{}".format(s["id"], lang.lower(), fmt)
                written.append(f)
        for s in cine_shots():
            f = finalize(s, "EN", fmt)
            f["id"] = "{}_{}".format(s["id"], fmt)
            written.append(f)
    portraits = {"id": "portraits", "scene": "Portraits", "lang": "EN", "portraitSize": 1024, "outDir": "Logs/Trailer/out"}
    written.append(portraits)
    for f in written:
        with open(os.path.join(OUT, f["id"] + ".json"), "w", encoding="utf-8") as fh:
            json.dump(f, fh, ensure_ascii=False, indent=1)
    print(len(written), "shots")


if __name__ == "__main__":
    sys.exit(main())
