import os, sys; sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), ".."))
import sys, math, random, png
from draw import *
OUT=sys.argv[1]; icons={}
D0,D1,D2,D3='0f380f','306230','8bac0f','9bbc0f'
def icon(name):
    def deco(f): icons[name]=f; return f
    return deco
FONT={'D':"##.|#.#|#.#|#.#|##.",'O':".#.|#.#|#.#|#.#|.#.",'U':"#.#|#.#|#.#|#.#|###",'B':"##.|#.#|##.|#.#|##.",'L':"#..|#..|#..|#..|###",
'E':"###|#..|##.|#..|###",'R':"##.|#.#|##.|#.#|#.#",'A':".#.|#.#|###|#.#|#.#",'G':".##|#..|#.#|#.#|.##",'N':"##.|#.#|#.#|#.#|#.#"}
def text(s,y):
    x=(32-(len(s)*4-1))//2; m=set()
    for ch in s:
        for r,row in enumerate(FONT[ch].split('|')):
            for k,v in enumerate(row):
                if v=='#': m.add((x+k,y+r))
        x+=4
    return m

FACE_TOP=["......CCCCCCCCCC","....CCCCCCCCCCCC","...CCCCCCCCWWWWW"]
def face(emblem,lower):
    return FACE_TOP+["..CCCCCCCCW"+e for e in emblem]+["..CCCCCCCCCWWWWW",".KKKKKKKKKKKKKKK",
     "..HHSSSSSSSSSSSS",".SHHSSSSSKKSSSSS",".SHHSSSSSKWSSSSS",".SHSSSSSSKKSSSSS","..SSSSSSSSSSSNNN","..SSSSSSSSSSNSSS",
     "..SSSSSSSSSSNSSS","..SSSSSSSSSSSNNN"]+lower
@icon('gb_super_mario_land')
def _(c):
    m=art(face(["MWWWW","MMWWW","MWMWW","MWWMM","MWWWW"],["..SSSSKKKKKKKKKK","...SSKKKKKKKKKKK","...SSSSKKKSSSSSS","....SSSSSSSSKKKK",".....SSSSSSSSSSS","......SSSSSSSSSS","........SSSSSSSS","....CCCCCCCCCCCC"]),0,1,mir=True)
    allm=set().union(*m.values()); c.fill(outline(allm),D0)
    for k,v in dict(C=D1,W=D3,M=D0,K=D0,H=D0,S=D2,N=D1).items(): c.fill(m.get(k,set()),v)
@icon('gb_wario_land')
def _(c):
    m=art(face(["MWWWW","MWWWW","MWWMM","MWMWW","MMWWW"],["..SSKKKKKKKKKKKK",".KKKKSSKKKKKKKKK","KK..KKSSSSKKKKKK","....SSKWWWWWWWWW","....SSKWKWKWKWKW",".....SSKKKKKKKKK","......SSSSSSSSSS","....CCCCCCCCCCCC"]),0,1,mir=True)
    allm=set().union(*m.values()); c.fill(outline(allm),D0)
    for k,v in dict(C=D1,W=D3,M=D0,K=D0,H=D0,S=D2,N=D1).items(): c.fill(m.get(k,set()),v)

@icon('gb_burai_fighter')
def _(c):
    c.fill(rect(0,0,31,31),D0); random.seed(5)
    for _ in range(14): c.put(random.randrange(32),random.randrange(32),random.choice([D1,D2]))
    c.fill(poly([(1,9),(11,7.5),(11,11.5)]),D2); c.fill(poly([(5,9.5),(11,8.5),(11,10.5)]),D3)
    c.fill(rect(11,7,18,11),D2); c.fill(edge(rect(11,7,18,11)),D1)
    c.fill(rect(10,12,21,16),D3); c.fill(rect(3,13,10,15),D3); c.fill(rect(1,14,3,16),D2)
    c.fill(disc(24,13,3.4),D3); c.fill(rect(24,12,27,13),D0); c.put(25,12,D3)
    c.fill(rect(19,17,29,18),D2); c.fill(rect(19,16,21,19),D2)
    for x in (24,28): c.fill(rect(x,22,x+1,22),D3)
    c.fill(disc(26,27,2.6),D1); c.fill(disc(26,27,1.2),D3)

@icon('gb_double_dragon')
def _(c):
    m=art(["..H.............","...H............","...HH...........","..DDDDD.........",".DDDDDDDD.......","DDDEDDDDDD......","DDDDDDDDDDDD....",".DDDDDDDDDDDDD..","..DDDDDDDDTDTD..","...DDDD.........","...DDD..TT.T....","...DDDDDDDDDD...","..DDDDDDDDDD....",".DDDDDD.........","DDDDDD.........."],0,1,mir=True)
    c.fill(m['H'],D1); c.fill(m['D'],D1); c.fill(edge(m['D']),D0); c.fill(m['E'],D3); c.fill(m['T'],D3)
    c.fill(disc(16,11,2.2),D0); c.fill(disc(16,11,1),D2)
    c.fill(text("DOUBLE",19),D0); c.fill(text("DRAGON",25),D0)

@icon('gb_castlevania')
def _(c):
    c.fill(rect(0,0,31,31),D1)
    c.fill(disc(22,8,5.6),D3); c.fill(disc(20,7,1.2)|disc(24,10,0.9),D2)
    for bx,by in ((19,6),(25,9),(9,4)):
        c.fill({(bx-2,by-1),(bx-1,by),(bx,by),(bx+1,by),(bx+2,by-1),(bx,by+1)},D0)
    cas=rect(1,20,30,31)|rect(3,12,8,20)|poly([(2.5,12.5),(5.5,6),(8.5,12.5)])|rect(13,8,18,20)|poly([(12.5,8.5),(15.5,1),(18.5,8.5)])
    cas|=rect(23,14,28,20)|poly([(22.5,14.5),(25.5,9),(28.5,14.5)])
    cas|={(x,19) for x in range(1,31,2)}|rect(9,17,12,20)|rect(19,17,22,20)
    c.fill(cas,D0)
    for x,y in ((5,14),(15,11),(15,15),(25,16),(10,24),(20,24),(5,26),(26,25)): c.fill(rect(x,y,x,y+1),D2)

@icon('gb_links_awakening')
def _(c):
    c.fill(rect(0,0,31,31),D3)
    blade=line(8,24,27,4,2.4); c.fill(dilate(blade)-blade,D0); c.fill(blade,D2)
    c.fill(line(4,22,10,28,1.8),D1); c.fill(line(2,30,7,25,1.8),D0)
    s=poly([(9,9),(23,9),(23,19),(16,28),(9,19)]); c.fill(outline(s),D0); c.fill(s,D1); c.fill(edge(s),D0)
    c.fill(poly([(11,11),(21,11),(21,18),(16,25),(11,18)])-s|set(),D1)
    c.fill(poly([(16,12),(19.5,18),(12.5,18)]),D3); c.fill(rect(15,19,16,22),D3)

@icon('gb_dr_mario')
def _(c):
    c.fill(rect(0,0,31,31),D3)
    a=line(5,13,11,7,7); b=line(11,7,17,1.5,7)
    cap=a|b; c.fill(cap,D1); c.fill(b-a,D3); c.fill(edge(cap)|line(8,4,14,10,1)&cap,D0)
    c.fill(rect(6,9,7,10),D2)
    v=disc(21,22,6.5)
    for k in range(8):
        r=math.radians(k*45+22); c.fill(line(21,22,21+9*math.cos(r),22+9*math.sin(r),1.6),D0)
    c.fill(v,D0)
    c.fill(ell(18.5,20,1.6,1.8)|ell(23.5,20,1.6,1.8),D3); c.put(19,20,D0); c.put(24,20,D0)
    c.fill(rect(18,24,24,25),D3); c.fill({(19,25),(21,25),(23,25)},D0)

for name,f in icons.items():
    c=C(D3); f(c); png.write(f'{OUT}/{name}.png',c.p)
print(len(icons))
