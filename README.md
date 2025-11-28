
![Projekt Banner](Assets/FRONTLINE.png)
# FRONTLINE 
## Áttekintés
A **Frontline** egy kétjátékos, körökre osztott stratégiai autobattler. A támadó célja a védő kastélyának elfoglalása, míg a védő feladata hét sikeres védelem végrehajtása. A pálya hexagon csempékből áll, a csata automatikusan zajlik.

A projekt Unity alapú. Ez a dokumentáció tartalmazza a játékmenet leírását, a fő rendszereket, a fájlszerkezetet és az assetek elérését.

---

## Tartalomjegyzék
1. Játékmenet összefoglaló  
2. Fő funkciók  
3. Játékszabályok  
4. Mentés / betöltés  
5. Assetek elérése  
6. Projektstruktúra  
7. Futtatás  
8. Verzióinformációk

---

## 1. Játékmenet összefoglaló
A játék maximum **14 körig** (SKELETON esetén vár elfoglalásig) tart. Egy kör:

1. Pálya generálása  
2. Egységelhelyezés (max. 20 pont)  
3. Automatikus csata  
4. Pontok frissítése  
5. Győzelmi feltételek vizsgálata

**Győzelem:**
- Támadó: elfoglalja a kastély mezőt  
- A game scene-en belül, ha elfoglaljuk az adott erődöt, ezzel jelezve a World Map-nek, hogy a csata befejeződött, és ez alapján elfoglalják a mezőt
- A nyilakkal lehetőségünk van gyorsítani a játékmenetet, ha a bal nyílra nyomunk a bal oldali játékos nyer, ha a jobb oldalira, akkor pedig a jobb oldali.

---

## 2. Fő funkciók
- Hatszög-alapú pályagenerálás  
- Kétjátékos mód  
- Egységpont-rendszer  
- Játékállás mentése és betöltése (.json)  
- Harci log  
- Körkezelés és győzelmi logika  

---

## 3. Játékszabályok

### Egységek
| Típus        | Sebesség | Támadás | Élet | Akt. HP |
|--------------|---------|---------|------|---------|
| Közelharcos  |    3f    | 1 | 5  | változó |

Egységstatisztikák:  
`Assets/Scripts/Unit.cs`

### Aranyrendszer
- Kör elején: **15 pont**  
- Győztes: Minden pontját + 5-öt visz tovább 
- Vesztes: maradék pontját továbbviszi  

---

## 4. Mentés és betöltés
A mentés JSON formátumú:
`Users\USER\AppData\LocalLow\DefaultCompany\FrontLineGame\saves\saveyymmdd_hhmm`

Mentett adatok:
- kör  
- pálya   
- játékosok pontjai  
- csatatörténet  

---

## 5. Assetek elérése (Unity – kötelező dokumentáció)

### Pálya
`Assets/Hexagons`

`Assets/Prefabs`

`Assets/Scenes`

A pályagenerálás logikája:  
`Assets/Scripts/Map/MapGenerator.cs`


### Grafika / UI
`Assets/Hexagons`

---

## 6. Projektstruktúra

- **📁Assets/**
  - **Hexagons/** – textúrák
  - **Prefabs/** – előre elkészített modellek
  - **Scenes/** – jelenetek, GameScene, WorldMapScene, MainMenu
  - **Scripts/** – az egész program "agya", tartalmazza mindenhez a scriptet
  - **Settings/** - settings, volume sliders, PC/Mobile Renders
  - **Tests/** - edit mode, play mode

---

## 7. Futtatás
`FrontLineGame.exe`

---

## 8. How-to
Ha a harcszimuláció nélkül szeretnénk lejátszani egy adott csatát, az alábbi parancsokkal átugorhatjuk:
  **→** (jobbra nyíl) - jobb oldal (védő) nyer
  **←** (balra nyíl) - bal oldal (támadó) nyer
Legutoljára lehelyezett egység visszavonása: {space}
###
A játék elindítása a main world map-re visz minket, itt a bal oldali játékos a támadó, ő választhatja ki, melyik területet támadja, amellyel szomszédos területei vannak
  {a játék kezdetekor ezek a felezővonaltól jobbra található közvetlen területek, ha a kurzort átmozgatjuk rajta, a highlight megmutatja a támadhatókat}
###
Egy adott csatán belül mindkét játékos lehelyezheti az egységeit a két oldalon található "Vásárolj Harcost Bal" és "Vásárolj Harcost Jobb" gombokkal.
Az egységek lehelyezése után a Csata indítása gomb lenyomásával indul a harc szimuláció. Amennyiben a harcot a bal oldali játékos nyeri, elfoglalja a támadott területet.
###
Játék vége: A támadó (bal) játékos elfoglalja a védekező (jobb) játékos kastélyát.

---

## 9. Verzióinformációk
- Játékverzió: 1.0 Alpha  
