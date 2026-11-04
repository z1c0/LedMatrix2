import os, sys; sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), ".."))
import sys, math, random, png
from draw import *
OUT=sys.argv[1]; icons={}
def icon(name):
    def deco(f): icons[name]=f; return f
    return deco
G={'N':"#.#|###|###|###|#.#",'E':"###|#..|##.|#..|###",'T':"###|.#.|.#.|.#.|.#.",'.':"#|.|.|.|#"[0:0] or ".|.|.|.|#",
   'J':"..#|..#|..#|#.#|.#.",'S':".##|#..|.#.|..#|##.",'5':"###|#..|##.|..#|##.",'3':"##.|..#|.#.|..#|##.",
   'R':"##.|#.#|##.|#.#|#.#",'#':"#.#|###|#.#|###|#.#",'0':"###|#.#|#.#|#.#|###",'1':".#.|##.|.#.|.#.|###"}
def text(s,x,y,sc=1):
    m=set()
    for ch in s:
        rows=G[ch].split('|')
        for r,row in enumerate(rows):
            for k,v in enumerate(row):
                if v=='#': m|=rect(x+k*sc,y+r*sc,x+k*sc+sc-1,y+r*sc+sc-1)
        x+=(len(rows[0])+1)*sc
    return m
def hexagon(cx,cy,r,rot=-90): return poly([(cx+r*math.cos(math.radians(rot+60*k)),cy+r*math.sin(math.radians(rot+60*k))) for k in range(6)])
def ngon(cx,cy,r,n,rot=-90): return poly([(cx+r*math.cos(math.radians(rot+360/n*k)),cy+r*math.sin(math.radians(rot+360/n*k))) for k in range(n)])
def cring(cx,cy,ro,ri): return disc(cx,cy,ro)-disc(cx,cy,ri)-poly([(cx,cy),(cx+20,cy-14),(cx+20,cy+14)])
def rrect(x0,y0,x1,y1): return rect(x0,y0,x1,y1)-{(x0,y0),(x1,y0),(x0,y1),(x1,y1)}
def split(m,x): return {p for p in m if p[0]<x},{p for p in m if p[0]>=x}

@icon('dotnet')
def _(c):
    c.fill(rrect(1,1,30,30),'512bd4')
    c.fill(text("NET",6,11,2)|rect(2,19,3,20),'ffffff')

@icon('csharp')
def _(c):
    h=hexagon(16,16,15.5); l,r=split(h,16); c.fill(l,'9b4f96'); c.fill(r,'68217a')
    c.fill({(x,y) for x,y in h if y>=23},'7a2a80')
    c.fill(cring(13,16,8.6,4.6),'ffffff'); c.fill(rect(21,11,22,21)|rect(25,11,26,21)|rect(19,13,28,14)|rect(19,18,28,19),'ffffff')

@icon('cpp')
def _(c):
    h=hexagon(16,16,15.5); l,r=split(h,16); c.fill(l,'659ad2'); c.fill(r,'00599c')
    c.fill({(x,y) for x,y in h if y>=23},'044f88')
    c.fill(cring(12,16,8.6,4.6),'ffffff')
    for px in (21,27): c.fill(rect(px-2,16,px+2,16)|rect(px,14,px,18),'ffffff')

@icon('python')
def _(c):
    blue=rrect(9,2,20,12)|rrect(3,9,15,20)|rect(9,8,20,12)
    yel=rrect(11,19,22,29)|rrect(16,11,28,22)|rect(11,19,22,23)
    blue-=dilate(yel); c.fill(blue,'3776ab'); c.fill(yel,'ffd43b')
    c.fill(edge(blue)&{(x,y) for x,y in blue if x>14 or y>17},'285580'); c.fill(edge(yel)&{(x,y) for x,y in yel if x>21 or y>26},'d8a810')
    c.fill(rect(11,4,12,5),'ffffff'); c.fill(rect(19,26,20,27),'ffffff')

def ribbon(c,dark,mid,light):
    a=poly([(2,10.5),(6,8),(23,22),(23,28.5)]); b=poly([(2,21.5),(6,24),(23,10),(23,3.5)])
    c.fill(poly([(2,10.5),(6,8),(6,24),(2,21.5)]),dark)
    c.fill(a,mid); c.fill(b,light)
    c.fill(poly([(21,3),(29,7),(29,25),(21,29)]),light); c.fill(rect(27,7,29,25),dark)
@icon('visual_studio')
def _(c): ribbon(c,'3a1a6a','5c2d91','865fc5')
@icon('vscode')
def _(c): ribbon(c,'0a4a8a','0065a9','1f9cf0')

@icon('javascript')
def _(c): c.fill(rect(1,1,30,30),'f7df1e'); c.fill(text("JS",15,18,2),'1a1a1a')
@icon('typescript')
def _(c): c.fill(rrect(1,1,30,30),'3178c6'); c.fill(text("TS",15,18,2),'ffffff')

@icon('rust')
def _(c):
    gear=disc(16,16,12)
    for k in range(16):
        a=math.radians(k*22.5); gear|=disc(16+13.2*math.cos(a),16+13.2*math.sin(a),1.6)
    c.fill(gear,'dea584'); c.fill(disc(16,16,9.2),'000000')
    c.fill(text("R",11,9,3),'dea584')

@icon('go_gopher')
def _(c):
    c.fill(disc(6,6,3.2)|disc(26,6,3.2),'00add8'); c.fill(disc(6,6,1.4)|disc(26,6,1.4),'008aac')
    f=ell(16,19,13,13); c.fill(f,'00add8'); c.fill(edge(f),'007a99')
    for x in (10,22): c.fill(disc(x,13,4.8),'ffffff'); c.fill(outline(disc(x,13,4.8))&f,'007a99'); c.fill(disc(x+1.2,13.5,2),'101010'); c.put(x,12,'ffffff')
    c.fill(rect(15,21,17,25),'ffffff'); c.fill(rect(16,21,16,25),'d0d0d0')
    c.fill(ell(16,19.5,3.6,2.2),'f0c8a0'); c.fill(rect(15,18,16,19),'3a2a20')

@icon('git')
def _(c):
    d=poly([(16,0.8),(31.2,16),(16,31.2),(0.8,16)]); c.fill(d,'f05032'); c.fill({(x,y) for x,y in d if x+y>=33},'c83a20')
    w=line(10,6,10,26,2)|line(10,11,20,18,2)|disc(10,11,2.6)|disc(10,24,2.6)|disc(20,18,2.6)
    c.fill(w&d,'ffffff')

@icon('docker')
def _(c):
    body=poly([(2,17),(26,17),(27,14),(31,13.5),(29.5,18),(26,24),(18,28),(10,28),(4,24)])
    c.fill(body,'2496ed'); c.fill(edge(body)&{(x,y) for x,y in body if y>20},'1a6ab8'); c.put(8,22,'ffffff')
    for row,(y,xs) in enumerate([(13,(5,9,13,17)),(9,(9,13,17)),(5,(13,))]):
        for x in xs: c.fill(rect(x,y,x+2,y+2),'2496ed'); c.fill(rect(x,y,x+2,y),'6ac0ff')
    c.fill(polyline([(0,30),(4,29),(8,30),(12,29),(16,30),(20,29),(24,30),(28,29),(31,30)]),'0a3a6a')

@icon('linux_tux')
def _(c):
    b=ell(16,17,10,13.5); c.fill(b,'1c1c24'); c.fill(edge(b),'4a4a5a')
    c.fill(ell(16,21,7,9)|ell(16,10,5,4),'f0f0f0')
    for x in (13,19): c.fill(ell(x,8.5,2,2.4),'ffffff'); c.fill(rect(x,8,x,9),'101010')
    c.fill(ell(16,12.5,3.6,1.8),'f0b020'); c.fill(rect(14,13,18,13),'c08010')
    c.fill(ell(9.5,29.5,5,2.2)|ell(22.5,29.5,5,2.2),'f0b020'); c.fill(ell(9.5,30.5,4,1)|ell(22.5,30.5,4,1),'c08010')

@icon('kubernetes')
def _(c):
    h=ngon(16,16.5,15.5,7); c.fill(h,'326ce5'); c.fill(edge(h),'1a4ab0')
    w=disc(16,16.5,7.2)-disc(16,16.5,5.2)|disc(16,16.5,2)
    for k in range(7):
        a=math.radians(-90+360/7*k); w|=line(16,16.5,16+10*math.cos(a),16.5+10*math.sin(a),1.6)
    c.fill(w&h,'ffffff'); c.fill(disc(16,16.5,1),'326ce5')

@icon('terminal')
def _(c):
    c.fill(rect(1,3,30,28),'3a3a44'); c.fill(rect(2,8,29,27),'0c0c10')
    for x,col in ((4,'ff5f56'),(7,'ffbd2e'),(10,'27c93f')): c.fill(rect(x,5,x+1,6),col)
    c.fill(polyline([(4,11),(7,14),(4,17)]),'50ff70'); c.fill(rect(9,17,13,17),'50ff70')
    random.seed(4)
    for y in (20,23,26):
        x=4
        while x<27:
            w=random.randint(2,6); 
            if x+w>28: break
            c.fill(rect(x,y,x+w-1,y),random.choice(['8890a0','6a7080','50a0ff'])); x+=w+2

@icon('java')
def _(c):
    c.fill(polyline([(13,15),(11,12),(14,9),(12,6),(14,3)],1.6),'e76f00')
    c.fill(polyline([(18,15),(16,12),(19,9),(17,6),(19,3)],1.6),'f89820')
    c.fill(disc(23.5,22,3.6)-disc(23.5,22,1.8),'5382a1')
    cup=poly([(7,17),(23,17),(21.5,27),(8.5,27)]); c.fill(cup,'5382a1'); c.fill(rect(7,17,22,18),'8ab0d0')
    c.fill({(x,y) for x,y in cup if x>=19},'3a6080')
    s=ell(15,29,12,1.8); c.fill(s,'5382a1'); c.fill(edge(s)&{(x,y) for x,y in s if y>=29},'3a6080')

def shield(c,l,r,glyph):
    s=poly([(3,1.5),(29,1.5),(26.8,26),(16,30.5),(5.2,26)]); a,b=split(s,16); c.fill(a,l); c.fill(b,r)
    m=text(glyph,11,7,3); c.fill(m,'ffffff'); c.fill({(x,y) for x,y in m if x>=16},'ebebeb')
@icon('html5')
def _(c): shield(c,'e34f26','f06529','5')
@icon('css3')
def _(c): shield(c,'1572b6','33a9dc','3')

@icon('bug')
def _(c):
    for y0,y1 in ((12,9),(18,18),(24,27)):
        c.fill(polyline([(10,y0),(5,y0-1),(2,y1)])|polyline([(22,y0),(27,y0-1),(30,y1)]),'9a9aa0')
    c.fill(polyline([(14,6),(11,3),(9,3)])|polyline([(18,6),(21,3),(23,3)]),'9a9aa0')
    c.fill(ell(16,8,4.5,3.4),'3a3a40')
    b=ell(16,18.5,8,10); c.fill(b,'d03020'); c.fill(edge(b),'801810'); c.fill(rect(16,10,16,28)&b,'501008')
    for x,y in ((12,14),(20,14),(11,21),(21,21),(14,25),(18,25)): c.fill(disc(x,y,1.3),'301008')
    c.put(13,7,'ffffff'); c.put(18,7,'ffffff')

@icon('stack_overflow')
def _(c):
    c.fill(polyline([(5,18),(5,28),(25,28),(25,18)],2.2),'bcbbbb')
    for i in range(5):
        a=math.radians(-i*11); x0,y0=9+i*1.2,25-i*4.2; d=(math.cos(a),math.sin(a)); n=(-d[1]*1.15,d[0]*1.15); L=10.5
        c.fill(poly([(x0-n[0],y0-n[1]),(x0+d[0]*L-n[0],y0+d[1]*L-n[1]),(x0+d[0]*L+n[0],y0+d[1]*L+n[1]),(x0+n[0],y0+n[1])]),'f48024')

@icon('vim')
def _(c):
    d=poly([(16,1),(31,16),(16,31),(1,16)]); c.fill(d,'1a5a1a'); c.fill(poly([(16,3.5),(28.5,16),(16,28.5),(3.5,16)]),'019733')
    v=poly([(7,8),(13,8),(13,10),(12,10),(16,19),(21,10),(20,10),(20,8),(26,8),(26,10),(25,10),(17,26),(14,26),(8,10),(7,10)])
    c.fill(outline(v),'0a2a0a'); c.fill(v,'e8e8e8'); c.fill({(x,y) for x,y in v if x>=17},'b0b0b0')

@icon('database')
def _(c):
    for i,y in enumerate((22,14,6)):
        body=rect(5,y,26,y+6)|ell(15.5,y+6,10.6,3); c.fill(body,'336791'); c.fill({(x,y2) for x,y2 in body if x>=21},'2a5478')
        top=ell(15.5,y,10.6,3); c.fill(top,'6a9ad0'); c.fill(edge(top),'336791')
    for y in (24,16,8): pass

@icon('binary')
def _(c):
    random.seed(1)
    for cx in range(0,32,4):
        head=random.randint(16,38); length=random.randint(22,36)
        for y in range(head-6,-6,-6):
            k=1-(head-y)/length
            if k<=0: break
            col='c0ffc0' if y==head-6 else '%02x%02x%02x'%(0,int(40+190*k),int(30*k))
            c.fill(text(random.choice('01'),cx,y),col)

@icon('raspberry_pi')
def _(c):
    c.fill(poly([(16,9),(9,2),(4,4),(8,8)]),'75a928'); c.fill(poly([(16,9),(23,2),(28,4),(24,8)]),'75a928')
    c.fill(poly([(16,9),(9,2),(8,8)])&rect(0,0,31,31)-poly([(16,9),(9,2),(4,4)]),'5a8a18')
    berry=poly([(6,12),(26,12),(27,19),(22,27),(16,30.5),(10,27),(5,19)])
    c.fill(berry,'7a0a2a')
    for y in range(12,31,4):
        off=2 if (y//4)%2 else 0
        for x in range(5+off,28,4):
            dd=disc(x,y+1.5,2.1)&berry
            c.fill(dd,'c51a4a'); c.fill({p for p in dd if p[0]<x and p[1]<y+1.5},'ff5a80')

@icon('ruby')
def _(c):
    g=poly([(8,5),(24,5),(30,12),(16,29.5),(2,12)]); c.fill(g,'cc342d')
    c.fill(poly([(8,5),(24,5),(20,12),(12,12)]),'f06a5a'); c.fill(poly([(2,12),(8,5),(12,12)]),'e04a3a'); c.fill(poly([(30,12),(24,5),(20,12)]),'a02018')
    c.fill(poly([(12,12),(20,12),(16,29.5)]),'e04a3a'); c.fill(poly([(20,12),(30,12),(16,29.5)]),'8a1810')
    c.fill(edge(g),'6a0c08'); c.put(13,7,'ffffff'); c.put(14,7,'ffd0d0')

for name,f in icons.items():
    c=C(); f(c); png.write(f'{OUT}/{name}.png',c.p)
print(len(icons))
