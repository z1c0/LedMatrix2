import os, sys; sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), ".."))
import sys, math, png
from draw import *
OUT=sys.argv[1]; icons={}
def icon(name):
    def deco(f): icons[name]=f; return f
    return deco
def rrect(x0,y0,x1,y1): return rect(x0,y0,x1,y1)-{(x0,y0),(x1,y0),(x0,y1),(x1,y1)}
def pix(c,rows,cols,x0=0,y0=0,s=1,mir=False):
    for r,row in enumerate(rows):
        if mir: row=row+row[::-1]
        for k,ch in enumerate(row):
            if ch in cols: c.fill(rect(x0+k*s,y0+r*s,x0+k*s+s-1,y0+r*s+s-1),cols[ch])
WOOD=('7a4418','5c3010','9a5a24')

# ── hardware ──
@icon('atari_logo')
def _(c):
    R='e01818'
    c.fill(rect(15,3,16,28),R)
    c.fill(polyline([(12,3),(12,12),(10,20),(6,26),(2,28)],2.2),R)
    c.fill(polyline([(19,3),(19,12),(21,20),(25,26),(29,28)],2.2),R)

@icon('atari_vcs_console')
def _(c):
    top=poly([(4,9),(27,9),(30,15),(1,15)])
    c.fill(top,'202020'); c.fill(rect(1,15,30,23),'181818')
    c.fill(rect(1,16,30,22),WOOD[0])
    for y in (17,19,21): c.fill(rect(1,y,30,y),WOOD[1])
    c.fill(rect(3,18,9,18)|rect(20,20,27,20),WOOD[2])
    c.fill(rect(9,6,22,10),'101010'); c.fill(rect(11,7,20,9),'303030')
    for x in (6,10,21,25): c.fill(rect(x,11,x+1,13),'b8b8b8'); c.put(x,11,'fcfcfc')
    c.fill(rect(14,12,17,14),'303030'); c.put(15,13,'e01818')
    c.fill(rect(1,24,30,25),'101010'); c.fill(rect(3,26,6,26)|rect(25,26,28,26),'303030')

@icon('atari_joystick')
def _(c):
    base=rrect(5,16,26,29); c.fill(base,'1a1a1a'); c.fill(edge(base),'4a4a4a')
    c.fill(rect(6,28,25,28),'101010')
    c.fill(ell(16,20,6,3),'303030'); c.fill(rect(15,5,17,20),'202020'); c.fill(rect(15,5,15,19),'505050')
    c.fill(disc(16,5,2.4),'202020'); c.put(15,4,'606060')
    c.fill(ell(9,17,2.6,1.6),'e01818'); c.put(8,16,'ff8080')
    c.fill(rect(19,26,24,26),'e08020')

@icon('atari_paddle')
def _(c):
    b=rrect(4,12,27,25); c.fill(b,'1a1a1a'); c.fill(edge(b),'4a4a4a')
    k=disc(13,18,6.5); c.fill(k,'303030'); c.fill(edge(k),'101010')
    for a in range(0,360,30):
        x=13+6*math.cos(math.radians(a)); y=18+6*math.sin(math.radians(a)); c.put(int(x),int(y),'606060')
    c.fill(disc(13,18,3),'a8a8a8'); c.fill(line(13,18,13,15),'101010')
    c.fill(rect(22,17,25,20),'e01818'); c.put(22,17,'ff8080')
    c.fill(polyline([(16,12),(18,7),(24,4),(31,4)]),'303030')

@icon('atari_cartridge')
def _(c):
    s=poly([(6,3),(25,3),(27,8),(27,29),(4,29),(4,8)]); c.fill(s,'202020'); c.fill(edge(s),'3a3a3a')
    for x in range(8,24,3): c.fill(rect(x,4,x+1,7),'101010')
    c.fill(rect(7,10,24,27),'e8c040'); c.fill(rect(7,10,24,12),'c03020')
    c.fill(rect(9,14,22,24),'1838a0')
    c.fill(disc(13,19,2.5),'e8c040'); c.fill(rect(17,16,20,17)|rect(16,21,21,22),'f8f8f8')
    c.fill(rect(7,25,24,26),'101010')

# ── games ──
@icon('atari_pong')
def _(c):
    W='e8e8e8'
    c.fill(rect(2,6,3,14)|rect(28,15,29,23),W)
    for y in range(1,32,4): c.fill(rect(15,y,16,y+1),'707070')
    c.fill(rect(20,10,21,11),W)
    pix(c,["###.###","..#.#.#","###.#.#","#...#.#","###.###"],{'#':W},4,0+1)
    pix(c,["###.###","..#.#..","###.###","#...#.#","###.###"],{'#':W},21,1)

@icon('atari_space_invader')
def _(c):
    pix(c,["..#.....#..","...#...#...","..#######..",".##.###.##.","###########","#.#######.#","#.#.....#.#","...##.##..."],{'#':'e8e040'},5,6,2)
    c.fill(rect(15,26,16,29),'40c040'); c.fill(rect(11,28,20,31),'40c040')

@icon('atari_ghost')
def _(c):
    g=rect(8,6,23,25)-rect(8,6,9,7)-rect(22,6,23,7)-{(8,8),(23,8),(10,6),(21,6)}
    g-=rect(10,24,11,25)|rect(15,24,16,25)|rect(20,24,21,25)
    c.fill(g,'d060d0'); c.fill({(x,y) for x,y in g if x%2==0 and y%4<2 and y>20},'a040a0')
    c.fill(rect(11,11,14,14)|rect(17,11,20,14),'f8f8f8'); c.fill(rect(13,12,14,13)|rect(19,12,20,13),'2020a0')
    for x in range(2,31,4): c.fill(rect(x,29,x+1,30),'e8c080')

@icon('atari_asteroids')
def _(c):
    r=poly([(6,4),(13,2),(19,5),(18,10),(21,14),(15,19),(8,17),(3,12)]); c.fill(edge(r),'c8a060')
    r2=poly([(22,20),(27,19),(30,23),(28,28),(23,29),(20,25)]); c.fill(edge(r2),'60a0e0')
    s=polyline([(9,27),(11,21),(13,27),(11,25.5),(9,27)]); c.fill(s,'f8f8f8')
    c.put(11,19,'f8f8f8'); c.put(11,16,'f8f8f8'); c.put(26,8,'f8f8f8'); c.put(2,23,'f8f8f8')

@icon('atari_pitfall')
def _(c):
    c.fill(rect(0,26,31,31),'b08020'); c.fill(rect(8,26,23,28),'000000'); c.fill(rect(9,28,22,29),'1858c8')
    c.fill(rect(0,0,31,2),'2a6a10'); c.fill(rect(4,0,5,25)|rect(26,0,27,25),'6a4010')
    c.fill(polyline([(16,1),(17,8),(20,14)]),'a08040')
    H=dict(h='40c040',s='e8b080',p='804010',b='202020')
    pix(c,["..hh","..ss",".hhh","hhhs",".hh.",".pp.","p..p","b..b"],H,18,13,1)
    pix(c,["#..#","####"],{'#':'30a030'},12,27,2)

@icon('atari_chalice')
def _(c):
    Y='e8d040'; D='a08820'
    cup=poly([(7,3),(25,3),(22,12),(18,15),(14,15),(10,12)]); c.fill(cup,Y); c.fill({(x,y) for x,y in cup if x>=19},D)
    c.fill(rect(15,15,17,23),Y); c.fill(rect(17,15,17,23),D); c.fill(rect(9,24,23,27),Y); c.fill(rect(9,27,23,27),D)
    c.fill(rect(10,4,11,7),'fcf8a0')
    for i,(x,y) in enumerate(((3,6),(28,10),(4,20),(27,22))): c.put(x,y,'fcf8a0')

@icon('atari_combat_tank')
def _(c):
    c.fill(rect(0,0,31,31),'486018')
    T=dict(t='e8b040',d='a07020')
    pix(c,["t.........","tttttttt..","tttttttt..","..tttttttt","..tttttttt","tttttttt..","tttttttt..","t........."],T,3,8,2)
    c.fill(rect(8,13,13,18),'a07020'); c.fill(rect(19,14,30,15),'e8b040')
    c.put(29,6,'f8f8f8')

@icon('atari_missile_command')
def _(c):
    c.fill(rect(0,28,31,31),'c8a020')
    for x in (3,10,19,26):
        c.fill(rect(x,25,x+2,27)|rect(x+1,24,x+1,24),'40a0e0')
    c.fill(rect(14,23,17,27),'c8a020')
    for a,b in (((4,0),(8,18)),((20,0),(23,14)),((30,2),(25,20))):
        c.fill(line(*a,*b),'e02020'); c.put(round(b[0]-.5),round(b[1]-.5),'f8f8f8')
    e=disc(16,12,4.5); c.fill(e,'e040e0'); c.fill(disc(16,12,2.5),'f8e060')
    c.fill(line(16,23,16,17),'40e040')

@icon('atari_river_raid')
def _(c):
    c.fill(rect(0,0,31,31),'1838b0'); c.fill(rect(0,0,5,31)|rect(26,0,31,31),'40a020')
    c.fill(rect(6,0,7,31)|rect(24,0,25,31),'60c030')
    J='e8e040'
    c.fill(rect(15,16,16,28)|rect(9,22,22,24)|rect(12,27,19,28)|{(15,15),(16,15)},J); c.fill(rect(15,18,16,20),'f8f8f8')
    fuel=rect(19,3,22,11); c.fill(fuel,'e02020');
    pix(c,["F","U","E","L"],{},0,0)
    for y in (4,6,8,10): c.fill(rect(20,y,21,y),'f8f8f8')
    c.fill(rect(9,6,13,7),'101010'); c.fill(rect(10,5,12,5)|rect(11,4,11,4),'e8e8e8')

@icon('atari_breakout')
def _(c):
    for i,col in enumerate(('d03030','e07020','c0a020','30a030','2060d0','5030b0')):
        y=2+i*2
        for x in range(1,31,4):
            if (i,x) not in ((3,13),(4,13),(5,13),(4,17),(5,9)): c.fill(rect(x,y,x+2,y),col)
    c.fill(rect(10,28,19,29),'d03030'); c.fill(rect(20,20,21,21),'f8f8f8')

@icon('atari_frogger')
def _(c):
    c.fill(rect(0,0,31,31),'101030'); c.fill(rect(0,15,31,15),'e8e8e8'); c.fill(rect(0,6,31,6)|rect(0,24,31,24),'606060')
    for x in range(0,32,6): c.fill(rect(x,15,x+2,15),'101030')
    pix(c,["#...#","#.#.#","#####",".###.","#####","#...#"],{'#':'40d040'},11,8,2)
    c.fill(rect(13,8,13,8)|rect(19,8,19,8),'e8e040')
    c.fill(rect(1,18,7,21),'e02020'); c.fill(rect(23,26,30,29),'e0a020'); c.fill(rect(24,1,30,4),'c040c0')

@icon('atari_et')
def _(c):
    c.fill(rect(0,22,31,31),'5a3a1a')
    h=ell(16,13,9,7); c.fill(h,'a07040'); c.fill(ell(16,22,5,4),'a07040')
    c.fill(ell(11,11,2.3,1.6)|ell(21,11,2.3,1.6),'2a6aff'); c.put(11,11,'f8f8f8'); c.put(21,11,'f8f8f8')
    c.fill(rect(12,16,20,16),'5a3a1a')
    c.fill(disc(25,25,2.2),'f84040'); c.put(25,24,'ffc0c0')
    for x,y in ((3,4),(8,2),(27,3),(29,8)): c.put(x,y,'f8f8f8')

@icon('atari_yars_revenge')
def _(c):
    for y in range(0,32,2):
        for x in range(13,20): c.put(x,y,['e02020','e0a020','40c040','2060e0','c040c0'][(x*3+y)%5])
    Y=dict(y='e8e040',d='a0a020')
    pix(c,["y.....y","yy...yy",".yyyyy.","..yyy..",".yyyyy.","yy.y.yy","y..y..y"],Y,1,11,2)
    pix(c,["..##..",".####.","######","######",".####.","..##.."],{'#':'e02080'},22,12,1)
    c.fill(rect(23,13,26,16),'f8f8f8')

@icon('atari_enduro')
def _(c):
    c.fill(rect(0,0,31,12),'4060c0'); c.fill(rect(0,13,31,31),'2a7a20')
    road=poly([(14,12),(18,12),(31,31),(1,31)]); c.fill(road,'606060')
    c.fill(line(16,12,6,31)|line(16,12,26,31),'e8e8e8')
    pix(c,[".rrrr.","r.rr.r","rrrrrr","b....b"],{'r':'e02020','b':'101010'},13,15,1)
    pix(c,[".pppp.","pppppp","p.pp.p","pppppp","bb..bb"],{'p':'a040e0','b':'101010'},10,24,2)
    c.fill(rect(18,6,22,9)|rect(5,4,8,6),'e0e0e0')

@icon('atari_kaboom_bomb')
def _(c):
    b=disc(15,19,9); c.fill(b,'202020'); c.fill(edge(b),'505050'); c.fill(disc(11,15,2),'707070')
    c.fill(rect(13,8,17,10),'505050')
    c.fill(polyline([(15,8),(17,4),(21,3),(23,5)]),'c0a060')
    c.fill(disc(24,4,2),'f8a020'); c.put(24,4,'f8f8a0'); c.put(27,2,'f8e040'); c.put(26,7,'f8e040'); c.put(22,1,'f8e040')

for name,f in icons.items():
    c=C(); f(c); png.write(f'{OUT}/{name}.png',c.p)
print(len(icons))
