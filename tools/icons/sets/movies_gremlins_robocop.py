import os, sys; sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), ".."))
import sys, math, png
from draw import *
OUT=sys.argv[1]; icons={}
def icon(name):
    def deco(f): icons[name]=f; return f
    return deco
def apply(c,m,cols):
    for k,v in cols.items():
        if k in m: c.fill(m[k],v)
def below(m,y0): return {(x,y) for x,y in m if y>=y0}
def right(m,x0): return {(x,y) for x,y in m if x>=x0}

# ───────────── Gremlins ─────────────
@icon('gremlins_gizmo')
def _(c):
    ear=poly([(10,12),(1,4),(0,10),(2,17),(10,19)])
    c.fill(mirror(ear),'a06a3a'); c.fill(mirror(poly([(9,13),(2,7),(2,11),(4,16),(9,17)])),'e89a9a')
    head=ell(16,18,10,10); c.fill(head,'f2e8d4')
    cap=ell(16,8,9,7)&head; c.fill(cap,'8a5228'); c.fill(ell(16,8,2,7)&head,'f2e8d4')
    c.fill(mirror(ell(7,22,3,4))&head,'8a5228')
    eye=mirror(ell(11.5,16.5,3.2,3.6)); c.fill(eye,'1a0c06')
    c.put(10,15,'ffffff'); c.put(20,15,'ffffff'); c.put(11,15,'ffffff'); c.put(21,15,'ffffff')
    c.fill(ell(16,22,1.8,1.2),'7a4040')
    c.fill(rect(14,25,17,25),'7a4040'); c.put(13,24,'7a4040'); c.put(18,24,'7a4040')
    c.fill(rect(9,28,22,31)-head,'8a5228'); c.fill(rect(13,28,18,31),'f2e8d4')

@icon('gremlins_stripe')
def _(c):
    ear=poly([(9,14),(0,7),(3,19),(9,21)])
    c.fill(mirror(ear),'2e6e22'); c.fill(mirror(poly([(8,15),(2,10),(4,18),(8,19)])),'b05050')
    head=ell(16,19,10,10); c.fill(head,'3c8a2c'); c.fill(below(edge(head),20),'24561a')
    for x,y in head:
        if (x*3+y*5)%11==0: c.put(x,y,'2e6e22')
    hawk=poly([(11,11),(12,2),(14,6),(16,0),(18,6),(20,2),(21,11)])
    c.fill(hawk,'f4f4f4'); c.fill(edge(hawk)&right(hawk,17),'b8b8c0')
    c.fill(mirror(line(8,14,14,16.5,2)),'1c4012')
    eye=mirror(ell(11,18,2.6,2.2)); c.fill(eye,'f0d020'); c.fill(mirror(rect(11,17,11,19)),'c01010')
    mouth=poly([(8,23),(24,23),(21,28),(11,28)]); c.fill(mouth,'4a0c0c')
    for x in range(9,24,3): c.fill(poly([(x,23),(x+2.6,23),(x+1.3,25.6)])&mouth,'fffff0')
    for x in range(12,21,3): c.fill(poly([(x,28),(x+2.6,28),(x+1.3,25.8)])&mouth,'fffff0')

@icon('gremlins_no_water')
def _(c):
    drop=poly([(16,4),(22,15),(10,15)])|disc(16,18,6.4)
    c.fill(drop,'2a8af0'); c.fill(edge(drop)&right(drop,17),'1460b8')
    c.fill(ell(13.5,18,1.4,2.6),'b0dcff')
    ring=disc(16,16,15.5)-disc(16,16,12.6); c.fill(ring,'e01818')
    c.fill(line(6.5,6.5,25.5,25.5,3.2)&disc(16,16,13),'e01818')

@icon('gremlins_midnight')
def _(c):
    moon=disc(26,5,4.8)-disc(23.6,3.4,3.9); c.fill(moon,'f0e090')
    face=disc(13,19,11.8); c.fill(face,'d8b040'); c.fill(disc(13,19,10.1),'f8f2e0')
    for h in range(12):
        a=math.radians(h*30); x=13+8.7*math.sin(a); y=19-8.7*math.cos(a)
        c.fill(disc(x,y,1.0 if h%3 else 1.3),'2a2a2a')
    c.fill(line(13,19,13,11.5,2),'1a1a1a'); c.fill(line(13,19,13.4,14,3),'1a1a1a')
    c.fill(disc(13,19,1.6),'c01010')

@icon('gremlins_cocoon')
def _(c):
    pod=ell(16,16,9,13.5); c.fill(pod,'5a6a1a')
    c.fill(right(pod,19)&pod-ell(15,16,7.4,12.5),'3a4410')
    c.fill(ell(12.5,11,2,5),'9aac3a'); c.put(12,7,'d0e070')
    for pts in [[(16,3),(14,10),(17,17),(15,25)],[(10,9),(12,16),(10,22)],[(22,8),(20,15),(22,23)]]:
        c.fill(polyline(pts)&pod,'2c3008')
    for x,l in [(9,4),(14,6),(19,3),(23,5)]:
        c.fill(line(x,24,x,26+l,2),'8aa02a'); c.fill(disc(x,26+l,1.2),'8aa02a')

# ───────────── RoboCop ─────────────
@icon('robocop_helmet')
def _(c):
    c.fill(rect(9,27,22,31),'5a6270'); c.fill(rect(5,29,26,31),'7a8494')
    helm=ell(16,12,11,11)|rect(5,12,26,20); c.fill(helm,'a8b4c8')
    c.fill(right(helm,21)-rect(0,22,31,31),'7a8698'); c.fill(ell(12,5,4,2),'e0e8f4')
    c.fill(rect(5,11,26,16),'101014'); c.fill(rect(6,12,25,12),'2a3a5a'); c.fill(rect(8,13,12,13),'6080b0')
    c.fill(mirror(ell(5,18,2.2,3)),'5a6270')
    jaw=poly([(9,17),(23,17),(22,24),(19,28),(13,28),(10,24)]); c.fill(jaw,'d8a080')
    c.fill(right(jaw,20),'b07a5c'); c.fill(rect(13,23,19,23),'7a3a2a')
    c.fill(mirror(poly([(5,17),(9,17),(10,24),(8,24)])),'8a96aa')

@icon('robocop_auto9')
def _(c):
    gun=poly([(2,9),(27,9),(28,11),(28,15),(2,15)])
    c.fill(gun,'5c6470'); c.fill(rect(2,9,27,10),'8c96a4')
    for x in range(5,17,3): c.fill(rect(x,12,x+1,13),'1a1c20')
    c.fill(rect(28,11,29,14),'3a3e46'); c.put(29,12,'101010')
    body=poly([(14,15),(30,15),(30,18),(14,18)]); c.fill(body,'4a5058')
    grip=poly([(22,18),(29,18),(31,30),(23,30)]); c.fill(grip,'2a2c32')
    for y in range(20,29,2): c.fill(line(23.5,y,29.5,y)&grip,'3e424a')
    c.fill(polyline([(21,18),(19,23),(23,24)],2),'4a5058')
    c.fill(rect(24,7,27,8),'8c96a4')

@icon('robocop_ed209')
def _(c):
    body=poly([(9,7),(23,7),(26,11),(25,17),(7,17),(6,11)]); c.fill(body,'c8c4b4')
    c.fill(right(body,17),'a09c8c'); c.fill(rect(10,9,22,11),'1a1a1a'); c.fill(rect(12,10,20,10),'a01010')
    for s in (0,1):
        f=(lambda x:x) if s==0 else (lambda x:31-x)
        arm={(f(x),y) for x,y in rect(1,9,6,16)}; c.fill(arm,'b0ac9c')
        c.fill({(f(x),y) for x,y in rect(2,16,4,22)},'5a5a5a'); c.fill({(f(x),y) for x,y in rect(2,22,4,23)},'202020')
        leg=polyline([(f(11),17),(f(8),23),(f(10),29)],4); c.fill(leg,'8a8676')
        c.fill({(f(x),y) for x,y in rect(6,29,13,30)},'6a665a')
    c.fill(rect(12,17,19,19),'6a665a')

@icon('robocop_ocp')
def _(c):
    c.fill(rect(0,7,31,24),'0c1830'); c.fill(rect(0,7,31,7)|rect(0,24,31,24),'4a6ab0')
    m=art([".##...##.###.",
           "#..#.#...#..#",
           "#..#.#...#..#",
           "#..#.#...###.",
           "#..#.#...#...",
           "#..#.#...#...",
           ".##...##.#..."])['#']
    big=set()
    for x,y in m: big|=rect(3+x*2,9+y*2,4+x*2,10+y*2)
    c.fill(big,'f0f4ff')

@icon('robocop_hud')
def _(c):
    c.fill(rect(0,0,31,31),'08141c')
    for y in range(0,32,2): c.fill(rect(0,y,31,y),'0c1e2a')
    c.fill(disc(16,9,3.6),'3a4a54'); c.fill(poly([(9,30),(10,15),(22,15),(23,30)]),'3a4a54')
    box=rect(7,3,25,30)-rect(8,4,24,29); corners={(x,y) for x,y in box if (x<11 or x>21) and (y<7 or y>26)}
    c.fill(corners,'30e060')
    c.fill(rect(15,7,17,11)-{(16,9)},'e02020'); c.fill(line(16,0,16,4)|line(16,13,16,15)|line(10,9,13,9)|line(19,9,22,9),'e02020')
    c.fill(rect(1,1,4,1)|rect(1,3,3,3)|rect(27,28,30,28)|rect(28,30,30,30),'30e060')

for name,f in icons.items():
    c=C(); f(c); png.write(f'{OUT}/{name}.png',c.p)
print(len(icons))
