import os, sys; sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), ".."))
import sys, math, random, png
from draw import *
OUT=sys.argv[1]; icons={}
def icon(name):
    def deco(f): icons[name]=f; return f
    return deco

def star(cx,cy,ro,ri,rot=-90):
    pts=[]
    for i in range(10):
        a=math.radians(rot+i*36); r=ro if i%2==0 else ri
        pts.append((cx+r*math.cos(a),cy+r*math.sin(a)))
    return poly(pts)

# ───────────────────────── Marvel ─────────────────────────
@icon('marvel_captain_america_shield')
def _(c):
    for r,col in [(15.6,'d01818'),(12.6,'f0f0f0'),(9.6,'d01818'),(6.6,'1838c0')]: c.fill(disc(16,16,r),col)
    c.fill(star(16,16.3,6.2,2.5),'f8f8f8')
    c.fill(edge(disc(16,16,15.6))&{(x,y) for x in range(32) for y in range(16,32)},'900c0c')

@icon('marvel_spiderman_mask')
def _(c):
    head=ell(16,16.5,13,15.5); c.fill(head,'d01818')
    cx,cy=16,15
    web=set()
    for a in range(0,360,30):
        r=math.radians(a); web|=line(cx,cy,cx+30*math.cos(r),cy+30*math.sin(r))
    for s in (0.38,0.68):
        web|=every(lambda x,y:abs(math.hypot((x-cx)/13,(y-cy)/15.5)-s)<0.035)
    c.fill(web&head,'400404')
    c.fill(edge(head),'700808')
    eye=poly([(3.5,9.5),(9,8),(14,12.5),(14,17.5),(9,17),(5,14)])
    eyes=mirror(eye); c.fill(outline(eyes)&head,'000000'); c.fill(eyes,'f4f4f4')

@icon('marvel_iron_man_helmet')
def _(c):
    m=art(["................",
           "..........RRRRRR",
           "........RRRRRRRR",
           "......SRRRRRRRRR",
           ".....SRRRRRRRRRR",
           "....SRRRRGGGGGGG",
           "....SRRRGGGGGGGG",
           "...SRRRGGGGGGGGG",
           "...SRRGGGGGGGGGG",
           "...SRRGGGGGGGGGG",
           "...SRRGGGGGGGGGG",
           "...SRGWWWWWGGGGG",
           "...SRGGWWWWWGGGG",
           "...SRGGGGGGGGGGG",
           "...SRRGGGGGGGGGG",
           "...SRRGGGGGGGGGG",
           "...SRRRGGGGGGGGG",
           "...SRRRGGGGGGGGG",
           "...SRRRRGGGGGGGG",
           "...SRRRRGGGGGGGG",
           "...SRRRRRGGGGDDD",
           "....SRRRRGGGGGGG",
           "....SRRRRRGGGGGG",
           ".....SRRRRGGGGGG",
           ".....SRRRRRGGGGG",
           "......SRRRRRGGGG",
           ".......SRRRRRRGG",
           "........SSRRRRRR",
           "..........SSSSSS"],0,1,mir=True)
    for k,col in dict(R='c01414',S='700808',G='e8b020',W='c8faff',D='6a4a10').items(): c.fill(m[k],col)
    c.fill(outline(m['G'])&m['R'],'8a0c0c')

@icon('marvel_mjolnir')
def _(c):
    c.fill(rect(4,3,27,15),'9aa0a8'); c.fill(rect(4,3,27,3),'d0d4da'); c.fill(rect(4,15,27,15),'5a5e66')
    c.fill(rect(4,3,4,15),'c0c4ca'); c.fill(rect(27,3,27,15),'60646c')
    for p in [(4,3),(27,3),(4,15),(27,15)]: c.put(*p,'000000')
    c.fill(rect(10,7,21,11)-rect(11,8,20,10),'6a6e76'); c.fill(rect(13,8,18,10),'7a7e86')
    c.fill(rect(14,16,17,26),'7a4a24'); c.fill(rect(14,16,14,26),'a06a38')
    for y in range(17,26,3): c.fill(rect(14,y,17,y),'4a2a12')
    c.fill(rect(13,27,18,28),'9aa0a8'); c.fill(rect(13,28,18,28),'5a5e66')
    c.fill(polyline([(15,29),(14,31),(17.5,31),(17,29)]),'5a3a1a')

@icon('marvel_infinity_gauntlet')
def _(c):
    G,L,D='d8a020','ffd860','6a4a10'
    hand=set()
    for x0,top in [(9,5),(13,3),(17,5),(21,8)]:
        f=rect(x0,top,x0+2,14)-{(x0,top),(x0+2,top)}; hand|=f
    hand|=rect(8,12,24,24)
    thumb=poly([(8,22),(2.5,12),(3,9),(6.5,9),(11,17)]); hand|=thumb
    cuff=rect(8,25,24,31)|rect(7,27,25,31)
    c.fill(hand,G); c.fill(cuff,'b08018')
    c.fill(outline(hand)|edge(hand),D); c.fill(rect(8,27,25,27),'7a5a12'); c.fill(rect(8,25,24,25),D)
    for x0,top in [(9,5),(13,3),(17,5),(21,8)]:
        c.fill(rect(x0,top+1,x0,11),L)
    for x in (12,16,20): c.fill(rect(x,4,x,12),D)
    for (x,col) in [(9,'a040ff'),(13,'2080ff'),(17,'ff2020'),(21,'ff8010')]:
        c.fill(rect(x,13,x+2,14),col)
    c.fill(rect(15,18,17,20),'ffe020'); c.fill(rect(4,11,5,12),'30e040')

@icon('marvel_avengers_logo')
def _(c):
    ring=disc(16,17,13.5)-disc(16,17,10.8)
    A=poly([(3,29),(8,29),(20.5,8),(20.5,29),(24,29),(24,2),(20,2)])|rect(12,19,28,21)
    A|=poly([(27,17.5),(31,20),(27,22.5)])
    c.fill(ring-dilate(A),'8a9098'); c.fill(A,'e8ecf0'); c.fill(edge(A)&{(x,y) for x,y in A if x>=22 or y>=21},'a0a6ae')

@icon('marvel_tesseract')
def _(c):
    T,R,B,L,M=(16,4),(27,10),(16,16),(5,10),(16,29)
    RB,LB=(27,23),(5,23)
    top=poly([T,R,B,L]); left=poly([L,B,M,LB]); right=poly([B,R,RB,M])
    cube=top|left|right
    c.fill(dilate(cube,2)-cube,'08183a'); c.fill(outline(cube),'1040a0')
    c.fill(top,'60c8ff'); c.fill(left,'2080e0'); c.fill(right,'1050b0')
    k=0.45; f=lambda p:(16+(p[0]-16)*k,16.5+(p[1]-16.5)*k)
    c.fill(polyline([f(T),f(R),f(RB),f(M),f(LB),f(L),f(T)])|polyline([f(L),f(B),f(R)])|line(*f(B),*f(M)),'e0faff')
    c.fill(polyline([T,R,RB,M,LB,L,T]),'a0e8ff'); c.fill(polyline([L,B,R])|line(*B,*M),'c0f0ff')

@icon('marvel_loki_helmet')
def _(c):
    m=art(["................",
           ".H..............",
           ".HH.............",
           ".HL.............",
           "..HL............",
           "..HL............",
           "..HHL...........",
           "...HL...........",
           "...HHL..........",
           "....HL..........",
           "....HHL.........",
           ".....HHL........",
           ".....HHHL.......",
           "......HHHGGGGGGG",
           ".......HGGGGGGGG",
           ".......GGGGGGGGG",
           "......GGGGGGGGGG",
           "......GGGGGGDDGG",
           "......GGGGGGGDDG",
           "......GGGGSSSSSG",
           "......GGGSSSSSSS",
           "......GGGSEESSSS",
           "......GGGSSSSSSS",
           "......GGGSSSSSSS",
           ".....GGGGSSSSSSS",
           ".....GGGNSSSSSSS",
           ".....GGGNNSSSMMS",
           "....VVGGNNNSSSSS",
           "...VVVVVGNNNNSSS",
           "..VVVVVVVVVNNNNN"],0,1,mir=True)
    for k,col in dict(H='e8b828',L='fff090',G='d0a020',D='7a5a10',S='e0c0a0',E='103a18',N='a08870',M='7a3030',V='1a6a28').items(): c.fill(m[k],col)

@icon('marvel_deadpool_mask')
def _(c):
    head=ell(16,16,12.5,14.8); c.fill(head,'c81414'); c.fill(edge(head),'6a0808')
    patch=mirror(ell(9.5,15,6,5.6))&head; c.fill(patch,'101010')
    c.fill(rect(15,1,16,30)&head-patch,'9a0e0e')
    eye=poly([(5.5,13.5),(12.5,15),(11.5,17.5),(6.5,16)]); c.fill(mirror(eye),'f4f4f4')

@icon('marvel_xmen_logo')
def _(c):
    c.fill(disc(16,16,15.6),'c01818'); c.fill(disc(16,16,13.6),'f0c000')
    X=every(lambda x,y:(abs((x-16)-(y-16))<2.9 or abs((x-16)+(y-16))<2.9) and math.hypot(x-16,y-16)<11.8)
    c.fill(X,'101010')

# ───────────────────────── DC ─────────────────────────
@icon('dc_batman_logo')
def _(c):
    e=ell(16,16.5,15.6,10.4); c.fill(e,'ffd020'); c.fill(edge(e),'000000')
    m=art(["............B...",
           "..B.........BB..",
           ".BBB........BBBB",
           "BBBBBB.....BBBBB",
           "BBBBBBBBBBBBBBBB",
           "BBBBBBBBBBBBBBBB",
           ".BBBBBBBBBBBBBBB",
           ".BB..BBBBBBBBBBB",
           ".B....BB.BBBBBBB",
           ".........BBBBBBB",
           "............BBBB",
           ".............BBB",
           "..............BB"],0,10,mir=True)
    m={'B':{(round(16+(x-15.5)*0.82-.5),y) for x,y in m['B']}}
    c.fill(m['B'],'000000')

@icon('dc_superman_logo')
def _(c):
    outer=poly([(1,7.5),(7.5,1.5),(24.5,1.5),(31,7.5),(16,30.5)])
    inner=poly([(4.5,8),(9,4),(23,4),(27.5,8),(16,26)])
    c.fill(outer,'d01818'); c.fill(inner,'ffd020')
    S=(rect(10,6,23,8)|rect(9,8,11,11)|rect(10,11,21,13)|rect(19,13,21,16)|rect(11,16,21,18)
       |rect(22,9,24,9)|rect(14,19,18,20))
    c.fill(S&inner,'d01818'); c.fill(edge(outer),'8a0c0c')

@icon('dc_wonder_woman_logo')
def _(c):
    W1=polyline([(2,6),(8,22),(16,8),(24,22),(30,6)],3.2)
    W2=polyline([(4,12),(10,28),(16,16),(22,28),(28,12)],3.2)
    c.fill(W2,'e0a828'); c.fill(outline(W1)&W2,'000000'); c.fill(W1,'ffd030')
    c.fill(edge(W1)&{(x,y) for x,y in W1 if y>15},'d8a020')

@icon('dc_flash_logo')
def _(c):
    c.fill(disc(16,16,15.6),'c81414'); c.fill(disc(16,16,12.8),'f4f4f4')
    bolt=poly([(18,2),(26,2),(19,13),(25.5,13),(8,30.5),(13.5,18),(7,18)])
    c.fill(outline(bolt)&disc(16,16,12.8),'c81414'); c.fill(bolt,'ffd020'); c.fill(edge(bolt)&{(x,y) for x,y in bolt if y>16},'e0a010')

@icon('dc_green_lantern_logo')
def _(c):
    sym=(disc(16,16,9.6)-disc(16,16,6.4))|rect(3,4,28,7)|rect(3,24,28,27)
    c.fill(dilate(sym,2)-sym,'0a2a10'); c.fill(outline(sym),'104a1a'); c.fill(sym,'30e848')
    c.fill(rect(3,4,28,4)|rect(3,24,28,24),'a0ffb0')

@icon('dc_bat_signal')
def _(c):
    for y in range(32):
        for x in range(32): c.put(x,y,'%02x%02x%02x'%(2,4,12+y//3))
    random.seed(7)
    for _ in range(12): c.put(random.randrange(32),random.randrange(0,14),'707890')
    beam=poly([(3,32),(8,32),(29,13),(13,6)])
    c.fill(beam,'3a3418')
    e=ell(21,10.5,9.6,6.6); c.fill(e,'ffe060'); c.fill(edge(e),'c8a830')
    bat=art(["...B.......B...",
             "BB.BB.....BB.BB",
             "BBBBBBBBBBBBBBB",
             ".BBBBBBBBBBBBB.",
             "..B..BBBBB..B..",
             "......BBB......"],14,8)
    c.fill(bat['B'],'000000')
    sky=[(0,6,24),(5,3,22),(9,5,26),(15,4,21),(20,6,25),(26,6,23)]
    for x0,w,top in sky:
        c.fill(rect(x0,top,x0+w-1,31),'0c0f18')
        for y in range(top+2,31,3):
            for x in range(x0+1,x0+w-1,2):
                if random.random()<0.3: c.put(x,y,'c8a030')

@icon('dc_robin_logo')
def _(c):
    c.fill(rect(0,0,31,31),'a01010')
    c.fill(disc(16,16,13.6),'101010'); c.fill(disc(16,16,11.8),'ffd020')
    R=art(["#####.","######","##..##","##..##","######","#####.","##.##.","##..##","##..##","##..##"],0,0)['#']
    c.fill({(9+x*2+dx,6+y*2+dy) for x,y in R for dx in (0,1) for dy in (0,1)},'101010')

@icon('dc_aquaman_trident')
def _(c):
    for y in range(32):
        for x in range(32): c.put(x,y,'%02x%02x%02x'%(0,20+y*2,50+y*3))
    random.seed(3)
    for _ in range(9):
        x,y=random.randrange(2,30),random.randrange(2,30)
        if not (6<=x<=25): c.put(x,y,'60b0e0')
    G,L,D='ffb020','ffe080','a06010'
    t=rect(15,11,16,31)|rect(8,10,23,12)|rect(8,4,9,10)|rect(22,4,23,10)|rect(15,3,16,10)
    t|=poly([(14,4),(16,0),(18,4)])|poly([(7,5),(8.5,1),(10,5)])|poly([(21,5),(22.5,1),(25,5)])
    t|={(6,5),(7,6),(25,5),(24,6),(13,4),(18,4)}
    c.fill(outline(t),'002030'); c.fill(t,G)
    c.fill(rect(15,3,15,31)|rect(8,4,8,10)|rect(22,4,22,10)|rect(8,10,23,10),L)
    c.fill(rect(16,13,16,31)|rect(9,12,23,12),D)
    for y in range(18,30,3): c.fill(rect(15,y,16,y),'7a4a10')

@icon('dc_kryptonite')
def _(c):
    rock=ell(16,30,14,4.5); c.fill(rock,'3a3a3e'); c.fill(edge(rock),'26262a')
    crystals=[([(12,27),(13,6),(16,2),(19,6),(20,27)],(16,2)),
              ([(5,27),(4,14),(6,11),(9,13),(11,27)],(6,11)),
              ([(20,27),(23,11),(26,9),(28,12),(27,27)],(26,9))]
    allc=set()
    for pts,_t in crystals: allc|=poly(pts)
    c.fill(dilate(allc,2)-allc-rock,'0a300a')
    for pts,tip in crystals:
        m=poly(pts); c.fill(m,'30d030')
        cx=sum(p[0] for p in pts)/len(pts)
        c.fill({(x,y) for x,y in m if x<cx-1},'a0ff80'); c.fill({(x,y) for x,y in m if x>cx+1.5},'108a20')
        c.fill(edge(m),'0a6a10')
    c.fill(rect(15,8,15,20),'e0ffd0')

@icon('dc_joker_haha')
def _(c):
    c.fill(rect(0,0,31,31),'24082e')
    for x,y,col in [(1,1,'60e830'),(17,11,'60e830'),(5,21,'60e830')]:
        m=glyph('H',x,y,2)|glyph('A',x+8,y,2)
        c.fill({(a+1,b+1) for a,b in m},'0a0a0a'); c.fill(m,col)

for name,f in icons.items():
    c=C(); f(c); png.write(f'{OUT}/{name}.png',c.p)
print(len(icons))
