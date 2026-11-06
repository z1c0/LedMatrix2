import os, sys; sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), ".."))
import sys, math, png
from draw import *
OUT=sys.argv[1]; icons={}
def icon(name):
    def deco(f): icons[name]=f; return f
    return deco
def below(m,y0): return {(x,y) for x,y in m if y>=y0}
def right(m,x0): return {(x,y) for x,y in m if x>=x0}

def ghost(cx,cy,s=1.0):
    body=ell(cx,cy,7*s,8*s)|poly([(cx-7*s,cy),(cx+7*s,cy),(cx+5*s,cy+11*s),(cx+2*s,cy+8*s),(cx-1*s,cy+12*s),(cx-4*s,cy+8*s),(cx-6*s,cy+11*s)])
    return body

# ───────────── Ghostbusters ─────────────
@icon('ghostbusters_logo')
def _(c):
    c.fill(disc(16,16,15.6),'f4f4f4')
    g=ghost(18,12,1.0); c.fill(outline(g)&disc(16,16,12.6),'101010'); c.fill(g&disc(16,16,12.6),'ffffff')
    c.fill(right(g,22)&disc(16,16,12.6)-edge(g),'d8d8e0')
    c.fill(ell(16.5,9.5,1.6,2.4)|ell(21.5,9.5,1.6,2.4),'101010')
    c.fill(ell(20,15,1.8,2.2),'101010')
    ring=disc(16,16,15.6)-disc(16,16,12.6); c.fill(ring,'e01818')
    c.fill(line(5.5,5.5,26.5,26.5,3.4)&disc(16,16,13),'e01818')

@icon('ghostbusters_slimer')
def _(c):
    glow=dilate(ghost(16,14,1.25)|ell(5,16,3,2.5)|ell(27,16,3,2.5),1)
    c.fill(glow,'1c5a10')
    body=ghost(16,14,1.25)|ell(5,16,3,2.5)|ell(27,16,3,2.5)
    c.fill(body,'60d030'); c.fill(right(body,21)-rect(0,0,31,3),'40a020'); c.fill(ell(11,7,3,2),'b0f080')
    c.fill(ell(12,10,2.4,2.8)|ell(20,10,2.4,2.8),'fff8c0'); c.fill(rect(12,10,12,11)|rect(20,10,20,11),'101010')
    mouth=ell(16,18,7,4.5); c.fill(mouth,'7a1020'); c.fill(ell(16,20,4,2)&mouth,'e04060')
    for x in range(10,23,3): c.fill(rect(x,14,x+1,15)&mouth,'fffff0')

@icon('ghostbusters_ecto1')
def _(c):
    body=poly([(1,16),(7,15),(10,9),(22,9),(24,14),(30,15),(31,17),(31,23),(1,23)])
    c.fill(body,'f0f0ec'); c.fill(below(body,21),'c8c8c4')
    c.fill(poly([(11,10),(16,10),(16,14),(9,14)])|poly([(17,10),(21,10),(23,14),(17,14)]),'405878')
    c.fill(rect(2,18,30,18),'d01818'); c.fill(poly([(24,15),(30,15),(30,13)]),'d01818')
    c.fill(rect(9,7,23,8),'a0a0a8'); c.fill(rect(14,5,17,6),'e02020'); c.fill(rect(11,6,12,6)|rect(20,6,21,6),'e0e0f0')
    gb=ghost(17,19,0.22); c.fill(disc(17,20,2.4),'e01818'); c.fill(disc(17,20,1.6),'ffffff')
    for wx in (7,25):
        c.fill(disc(wx,24,3.6),'1a1a1a'); c.fill(disc(wx,24,1.6),'b0b0b8')
    c.fill(rect(0,17,1,19),'f0e060'); c.fill(rect(30,19,31,21),'e02020')

@icon('ghostbusters_trap')
def _(c):
    beam=poly([(9,22),(23,22),(30,0),(2,0)]); c.fill(beam,'2a3070')
    c.fill(poly([(12,22),(20,22),(25,0),(7,0)]),'6070e0'); c.fill(poly([(14,22),(18,22),(19,0),(13,0)]),'d0e0ff')
    c.fill(poly([(6,22),(15,22),(9,18)]),'7a7a80'); c.fill(poly([(17,22),(26,22),(23,18)]),'7a7a80')
    box=rect(4,22,27,29); c.fill(box,'4a4a50'); c.fill(rect(4,22,27,22),'8a8a90')
    for x in range(4,28):
        for y in range(25,28):
            if ((x+y)//2)%2==0: c.put(x,y,'f0c020')
            else: c.put(x,y,'1a1a1a')
    c.fill(rect(4,30,27,30),'2a2a2e'); c.fill(polyline([(27,28),(30,30),(31,31)],1),'d01818')
    c.put(6,23,'e02020'); c.put(25,23,'30e060')

@icon('ghostbusters_stay_puft')
def _(c):
    head=ell(16,19,11,10); c.fill(head,'f8f8f0'); c.fill(right(head,23),'d8d8d0')
    hat=ell(16,9,10,4)|rect(8,4,24,9); c.fill(hat,'f8f8f0'); c.fill(rect(6,9,26,11)&ell(16,10,11,3),'2a50c0')
    c.fill(rect(9,4,23,4),'e0e0d8')
    c.fill(rect(11,16,12,18)|rect(20,16,21,18),'101010')
    c.fill(ell(8,21,2,1.4)|ell(24,21,2,1.4),'f0a0a0')
    smile=(ell(16,21,6,4.5)-ell(16,19.5,6,4.5))&below(ell(16,21,6,4.5),21); c.fill(smile,'101010')
    c.fill(poly([(9,28),(23,28),(19,31),(13,31)]),'e02020'); c.fill(poly([(14,28),(18,28),(16,31)]),'2a50c0')

for name,f in icons.items():
    c=C(); f(c); png.write(f'{OUT}/{name}.png',c.p)
print(len(icons))
