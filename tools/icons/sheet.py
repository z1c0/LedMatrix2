import sys,png
fs=sys.argv[2:]; S=8; G=8; W=len(fs)*(32*S+G)+G; H=32*S+2*G
c=[[(60,60,60,255)]*W for _ in range(H)]
for i,f in enumerate(fs):
    _,_,_,px=png.read(f); ox=G+i*(32*S+G)
    for y in range(32*S):
        for x in range(32*S):
            p=px[y//S][x//S]; c[G+y][ox+x]=p if p[3]>=50 else (0,0,0,255)
png.write(sys.argv[1],c)
