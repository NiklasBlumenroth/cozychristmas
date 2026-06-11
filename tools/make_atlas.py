from PIL import Image, ImageDraw, ImageFont
import math, os

SRC = r"C:\Users\Ic3Dr4gon\Desktop\GameDevelopment\cozychristmas\Assets\RoyalCod\ModularConstructor\Textures\atlas.png"
OUTDIR = r"C:\Users\Ic3Dr4gon\Desktop\GameDevelopment\cozychristmas\Assets\_Project\Textures"
os.makedirs(OUTDIR, exist_ok=True)
OUT = os.path.join(OUTDIR, "atlas_custom.png")
GUIDE = os.path.join(os.path.dirname(__file__), "covers_guide.png")

SCALE = 8                 # 256 -> 2048 px
W = H = 256 * SCALE
SS = 4                    # Supersampling fuer glatte Symbole
WHITE = (255, 255, 255)

# ---------- parametrische Symbole (d,cx,cy,s,c,bg) ----------
def star(d,cx,cy,s,c,bg):
    pts=[(cx+(s if i%2==0 else s*0.42)*math.cos(-math.pi/2+i*math.pi/5),
          cy+(s if i%2==0 else s*0.42)*math.sin(-math.pi/2+i*math.pi/5)) for i in range(10)]
    d.polygon(pts,fill=c)
def tree(d,cx,cy,s,c,bg):
    d.rectangle([cx-s*0.1,cy+s*0.55,cx+s*0.1,cy+s*0.85],fill=c)
    for yt,yb,hw in [(-0.9,-0.2,0.45),(-0.45,0.3,0.62),(0.1,0.6,0.8)]:
        d.polygon([(cx,cy+s*yt),(cx-s*hw,cy+s*yb),(cx+s*hw,cy+s*yb)],fill=c)
def snow(d,cx,cy,s,c,bg):
    L=s*0.95; w=max(2,int(s*0.09))
    for k in range(6):
        a=k*math.pi/3; d.line([cx,cy,cx+L*math.cos(a),cy+L*math.sin(a)],fill=c,width=w)
        for t in (0.55,0.82):
            bx,by=cx+L*t*math.cos(a),cy+L*t*math.sin(a); bl=L*0.24
            for sg in (1,-1):
                aa=a+sg*math.pi/3
                d.line([bx,by,bx+bl*math.cos(aa),by+bl*math.sin(aa)],fill=c,width=w)
def gift(d,cx,cy,s,c,bg):
    d.rectangle([cx-s*0.65,cy-s*0.35,cx+s*0.65,cy+s*0.8],fill=c)
    d.rectangle([cx-s*0.1,cy-s*0.35,cx+s*0.1,cy+s*0.8],fill=bg)
    d.rectangle([cx-s*0.65,cy+s*0.15,cx+s*0.65,cy+s*0.32],fill=bg)
    d.polygon([(cx,cy-s*0.35),(cx-s*0.45,cy-s*0.75),(cx-s*0.05,cy-s*0.5)],fill=c)
    d.polygon([(cx,cy-s*0.35),(cx+s*0.45,cy-s*0.75),(cx+s*0.05,cy-s*0.5)],fill=c)
def bauble(d,cx,cy,s,c,bg):
    d.ellipse([cx-s*0.6,cy-s*0.35,cx+s*0.6,cy+s*0.85],fill=c)
    d.rectangle([cx-s*0.14,cy-s*0.55,cx+s*0.14,cy-s*0.3],fill=c)
    d.arc([cx-s*0.12,cy-s*0.8,cx+s*0.12,cy-s*0.5],180,360,fill=c,width=max(2,int(s*0.08)))
def heart(d,cx,cy,s,c,bg):
    raw=[]
    for i in range(220):
        t=math.pi*2*i/220
        x=16*math.sin(t)**3
        y=13*math.cos(t)-5*math.cos(2*t)-2*math.cos(3*t)-math.cos(4*t)
        raw.append((x,y))
    xs=[p[0] for p in raw]; ys=[p[1] for p in raw]
    cxr=(min(xs)+max(xs))/2; cyr=(min(ys)+max(ys))/2
    k=(1.7*s)/max(max(xs)-min(xs), max(ys)-min(ys))
    d.polygon([(cx+(x-cxr)*k, cy-(y-cyr)*k) for x,y in raw],fill=c)
def bell(d,cx,cy,s,c,bg):
    d.polygon([(cx,cy-s*0.7),(cx-s*0.55,cy+s*0.35),(cx+s*0.55,cy+s*0.35)],fill=c)
    d.ellipse([cx-s*0.6,cy+s*0.2,cx+s*0.6,cy+s*0.5],fill=c)
    d.ellipse([cx-s*0.12,cy+s*0.45,cx+s*0.12,cy+s*0.7],fill=c)
def candle(d,cx,cy,s,c,bg):
    d.rectangle([cx-s*0.22,cy-s*0.3,cx+s*0.22,cy+s*0.8],fill=c)
    d.ellipse([cx-s*0.18,cy-s*0.75,cx+s*0.18,cy-s*0.3],fill=c)
    d.line([cx,cy-s*0.3,cx,cy-s*0.45],fill=bg,width=max(2,int(s*0.06)))
def snowman(d,cx,cy,s,c,bg):
    d.ellipse([cx-s*0.55,cy+s*0.05,cx+s*0.55,cy+s*0.9],fill=c)
    d.ellipse([cx-s*0.4,cy-s*0.4,cx+s*0.4,cy+s*0.3],fill=c)
    d.ellipse([cx-s*0.28,cy-s*0.85,cx+s*0.28,cy-s*0.3],fill=c)
def key(d,cx,cy,s,c,bg):
    d.ellipse([cx-s*0.7,cy-s*0.45,cx-s*0.05,cy+s*0.2],fill=c)
    d.ellipse([cx-s*0.5,cy-s*0.25,cx-s*0.25,cy],fill=bg)
    d.rectangle([cx-s*0.1,cy-s*0.12,cx+s*0.75,cy+s*0.12],fill=c)
    d.rectangle([cx+s*0.5,cy+s*0.1,cx+s*0.6,cy+s*0.4],fill=c)
    d.rectangle([cx+s*0.66,cy+s*0.1,cx+s*0.76,cy+s*0.45],fill=c)
def candy(d,cx,cy,s,c,bg):
    w=max(3,int(s*0.40)); r=w/2
    rh=0.33*s; hx=cx; hy=cy-0.34*s; stem_x=hx-rh; bot=cy+0.82*s
    pts=[(stem_x,bot),(stem_x,hy)]
    for ang in range(180,-1,-12):
        a=math.radians(ang); pts.append((hx+rh*math.cos(a), hy-rh*math.sin(a)))
    d.line(pts,fill=c,width=w,joint="curve")
    d.ellipse([stem_x-r,bot-r,stem_x+r,bot+r],fill=c)        # rundes Ende unten
    d.ellipse([hx+rh-r,hy-r,hx+rh+r,hy+r],fill=c)            # rundes Ende am Haken
def gingerbread(d,cx,cy,s,c,bg):
    d.ellipse([cx-0.22*s,cy-0.72*s,cx+0.22*s,cy-0.28*s],fill=c)
    d.rounded_rectangle([cx-0.26*s,cy-0.32*s,cx+0.26*s,cy+0.35*s],radius=int(s*0.18),fill=c)
    d.rounded_rectangle([cx-0.62*s,cy-0.28*s,cx-0.18*s,cy-0.02*s],radius=int(s*0.1),fill=c)
    d.rounded_rectangle([cx+0.18*s,cy-0.28*s,cx+0.62*s,cy-0.02*s],radius=int(s*0.1),fill=c)
    d.rounded_rectangle([cx-0.26*s,cy+0.2*s,cx-0.02*s,cy+0.68*s],radius=int(s*0.1),fill=c)
    d.rounded_rectangle([cx+0.02*s,cy+0.2*s,cx+0.26*s,cy+0.68*s],radius=int(s*0.1),fill=c)

# ---------- Auswahl ----------
SYMBOLS = [("Stern",star),("Tannenbaum",tree),("Schneeflocke",snow),("Geschenk",gift),
           ("Christbaumkugel",bauble),("Herz",heart),("Glocke",bell),("Kerze",candle),
           ("Schneemann",snowman),("Schluessel",key)]                       # 10 Reihen
def hx(h): return tuple(int(h[i:i+2],16) for i in (0,2,4))
COLORS = [("Tannenrot","9E2B25"),("Tannengruen","2E6B3E"),("Mitternachtsblau","2A4A7A"),
          ("Salbeigruen","7C9A6E"),("Beere","B23A6E"),("Anthrazit","3A3A42"),
          ("Karamell","B5793A"),("Schokobraun","6B4A2E")]                    # 8 Spalten

COLS, ROWS = len(COLORS), len(SYMBOLS)
ORIGIN_X, ORIGIN_Y = 128*SCALE, 96*SCALE
S = (W - ORIGIN_X)//COLS          # 128 px
assert S*COLS <= W-ORIGIN_X and S*ROWS <= H-ORIGIN_Y, "Raster passt nicht in freien Bereich"

atlas = Image.open(SRC).convert("RGBA").resize((W,H), Image.NEAREST)

def tile_img(base, fn):
    R=S*SS; t=Image.new("RGBA",(R,R),base+(255,)); dd=ImageDraw.Draw(t)
    fn(dd,R/2,R/2,R*0.34,WHITE+(255,),base+(255,))
    return t.resize((S,S),Image.LANCZOS)

def tile_rect(w, h, base, fn):
    t=Image.new("RGBA",(w*SS,h*SS),base+(255,)); dd=ImageDraw.Draw(t)
    fn(dd, w*SS/2, h*SS/2, min(w,h)*SS*0.34, WHITE+(255,), base+(255,))
    return t.resize((w,h),Image.LANCZOS)

rows=[]
for ri,(sname,fn) in enumerate(SYMBOLS):
    for ci,(cname,chex) in enumerate(COLORS):
        base=hx(chex); x=ORIGIN_X+ci*S; y=ORIGIN_Y+ri*S
        atlas.alpha_composite(tile_img(base,fn),(x,y))
        u0,u1=x/W,(x+S)/W; v0,v1=1-(y+S)/H,1-y/H
        rows.append((sname,cname,ci,ri,(x,y),(round(u0,4),round(v0,4),round(u1,4),round(v1,4)),
                     (round((u0+u1)/2,4),round((v0+v1)/2,4))))

# ---------- Streifen ueber dem Raster: 2 Zusatz-Symbolreihen + Vollton-Band ----------
# Layout je Spalte (von oben): Symbolreihe A | schmales Vollton-Band (v=0.6875) | Symbolreihe B.
# Das Band bei v=0.6875 bleibt rein farbig -> bestehende Buchkoerper-UVs unveraendert gueltig.
SW_X0 = 144*SCALE
SWW = (W - SW_X0)//COLS
EXTRA = [("Lebkuchenmann", gingerbread, 64*SCALE, 78*SCALE),
         ("Zuckerstange",  candy,       82*SCALE, 96*SCALE)]
BAND_Y0, BAND_Y1 = 78*SCALE, 82*SCALE
dd = ImageDraw.Draw(atlas)
extra=[]; solids=[]
for ci,(cname,chex) in enumerate(COLORS):
    base=hx(chex); x0=SW_X0+ci*SWW
    for sname,fn,y0,y1 in EXTRA:
        atlas.alpha_composite(tile_rect(SWW, y1-y0, base, fn),(x0,y0))
        u0,u1=x0/W,(x0+SWW)/W; v0,v1=1-y1/H,1-y0/H
        extra.append((sname,cname,(round(u0,4),round(v0,4),round(u1,4),round(v1,4)),
                      (round((u0+u1)/2,4),round((v0+v1)/2,4))))
    dd.rectangle([x0,BAND_Y0,x0+SWW-1,BAND_Y1-1],fill=base+(255,))  # Vollton-Band
    solids.append((cname,round((x0+SWW/2)/W,4),round(1-((BAND_Y0+BAND_Y1)/2)/H,4)))

atlas.convert("RGB").save(OUT)
print("Atlas gespeichert:",OUT,f"{W}x{H}, {len(rows)} Raster-Kacheln + {len(extra)} Zusatzkacheln")
print("\nZusatz-Symbole (Lebkuchenmann/Zuckerstange) je Farbe – UV-Rect (u0,v0,u1,v1) + Mitte:")
for sname,cname,uv,uc in extra:
    print(f"  {sname:14s} {cname:16s} {uv}  Mitte {uc}")
print("\nVollton-Band fuer Buchkoerper (unveraendert, UV-Mitte):")
for cname,uc,vc in solids:
    print(f"  {cname:16s} u={uc}  v={vc}")

# ---------- Uebersichtskarte ----------
try:
    Fb=ImageFont.truetype(r"C:\Windows\Fonts\arialbd.ttf",18); Fs=ImageFont.truetype(r"C:\Windows\Fonts\arial.ttf",13)
except Exception:
    Fb=ImageFont.load_default(); Fs=Fb
TH=120; LBL=150; HEAD=70
gw=LBL+COLS*TH; gh=HEAD+ROWS*TH
g=Image.new("RGB",(gw,gh),(245,245,245)); dg=ImageDraw.Draw(g)
for ci,(cname,chex) in enumerate(COLORS):
    x=LBL+ci*TH; dg.rectangle([x+8,8,x+TH-8,40],fill=hx(chex))
    dg.text((x+8,44),cname,font=Fs,fill=(20,20,20))
for ri,(sname,fn) in enumerate(SYMBOLS):
    y=HEAD+ri*TH; dg.text((8,y+TH//2-8),sname,font=Fb,fill=(20,20,20))
    for ci,(cname,chex) in enumerate(COLORS):
        th=tile_img(hx(chex),fn).resize((TH-10,TH-10),Image.LANCZOS)
        g.paste(th,(LBL+ci*TH+5,y+5))
g.save(GUIDE); print("Karte gespeichert:",GUIDE,g.size)
