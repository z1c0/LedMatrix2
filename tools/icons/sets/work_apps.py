import os, sys; sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), ".."))
import sys, math, png
from draw import *
OUT=sys.argv[1]; icons={}
def icon(name):
    def deco(f): icons[name]=f; return f
    return deco
def pill(x0,y0,x1,y1):
    r=min(x1-x0,y1-y0)/2; cx0,cy0,cx1,cy1=x0+r,y0+r,x1-r,y1-r
    return every(lambda x,y:(max(cx0-x,0,x-cx1))**2+(max(cy0-y,0,y-cy1))**2<=r*r)
def rsq(x0,y0,x1,y1,r): return every(lambda x,y:(max(x0+r-x,0,x-(x1-r)))**2+(max(y0+r-y,0,y-(y1-r)))**2<=r*r)

@icon('work_slack')
def _(c):
    B,G,Y,R='36c5f0','2eb67d','ecb22e','e01e5a'
    c.fill(pill(2,9,16,15)|pill(10,2,16,8),B)      # top-left: bar left, nub up
    c.fill(pill(16,2,22,16)|pill(24,10,30,16),G)   # top-right: bar up, nub right
    c.fill(pill(16,16,30,22)|pill(16,24,22,30),Y)  # bottom-right: bar right, nub down
    c.fill(pill(10,16,16,30)|pill(2,16,8,22),R)    # bottom-left: bar down, nub left
    for m,col in ((rect(10,6,15,8),B),(rect(24,10,25,15),G),(rect(16,24,21,25),Y),(rect(6,16,7,21),R)): c.fill(m,col)
@icon('work_zoom')
def _(c):
    c.fill(rsq(1,1,31,31,7),'2d8cff')
    c.fill(rsq(6,10,21,23,2.5),'ffffff')
    c.fill(poly([(21,15),(26,11),(26,22),(21,18)]),'ffffff')

@icon('work_outlook')
def _(c):
    env=rect(13,8,30,25); c.fill(env,'28a8ea'); c.fill(edge(env),'0f78d4')
    c.fill(polyline([(13,8),(21.5,16),(30,8)],1.6),'0f78d4')
    c.fill(rect(13,17,30,25),'1490df'); c.fill(polyline([(13,25),(21.5,18),(30,25)]),'0f78d4')
    t=rect(2,5,19,27); c.fill(t,'0a5fb4'); c.fill(edge(t),'084a8c')
    o=ell(10.5,16,5,7)-ell(10.5,16,2.6,4.4); c.fill(o,'ffffff')

@icon('work_jira')
def _(c):
    c.fill(rsq(1,1,31,31,6),'0052cc'); c.fill(rsq(1,1,31,16,6)&rect(0,0,31,15),'2684ff')
    F={'J':"..#|..#|..#|#.#|###",'I':"###|.#.|.#.|.#.|###",'R':"##.|#.#|##.|#.#|#.#",'A':".#.|#.#|###|#.#|#.#"}
    for i,ch in enumerate('JIRA'):
        for r,row in enumerate(F[ch].split('|')):
            for k,v in enumerate(row):
                if v=='#': c.fill(rect(1+i*8+k*2,11+r*2,2+i*8+k*2,12+r*2),'ffffff')

for name,f in icons.items():
    c=C(); f(c); png.write(f'{OUT}/{name}.png',c.p)
print(len(icons))
