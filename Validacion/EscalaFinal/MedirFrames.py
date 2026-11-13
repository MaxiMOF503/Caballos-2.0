from pathlib import Path
from PIL import Image
import json
p=Path(r'C:\Caballos\Caballos-2.0');rows=json.loads((p/'Catalogos/Animaciones/Produccion.json').read_text(encoding='utf-8'));out=[]
for row in rows:
 im=Image.open(p/row['archivo']).convert('RGBA');w,h=im.size;boxes=[]
 for i in range(8):
  crop=im.crop((round(i%4*w/4),round(i//4*h/2),round((i%4+1)*w/4),round((i//4+1)*h/2)))
  b=crop.getchannel('A').point(lambda x:255 if x>=128 else 0).getbbox();cw,ch=crop.size
  boxes.append((b[0]/cw,1-b[3]/ch,b[2]/cw,1-b[1]/ch))
 x=min(b[0] for b in boxes);y=min(b[1] for b in boxes);right=max(b[2] for b in boxes);top=max(b[3] for b in boxes)
 out.append(dict(id=row['id'],bounds=dict(x=x,y=y,width=right-x,height=top-y)))
(p/'Assets/Resources/LimitesVisualesCarrera.json').write_text(json.dumps(dict(entries=out),indent=2)+'\n',encoding='utf-8')
