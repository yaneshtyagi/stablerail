from pathlib import Path
import re,json
from pypdf import PdfReader
import pdfplumber,pypdfium2 as pdfium
from PIL import Image,ImageDraw
root=Path(__file__).resolve().parents[3];pdf=root/'output/pdf/StableRail_Domain_Guide.pdf';reader=PdfReader(pdf)
source=(root/'DOMAIN_GUIDE.md').read_text()
assert len(reader.pages)==25, f'Expected 25 pages, found {len(reader.pages)}'
texts=[p.extract_text() for p in reader.pages];text='\n'.join(texts)
for expected in ['249.20','93.75%','CASE-1042','STMT-002','12 / 64','Answer key and reasoning','Glossary: A-M','Glossary: N-Z']:
 assert expected in text, f'Missing expected teaching detail: {expected}'
assert '\ufffd' not in text
assert len(reader.outline)>=25
bad=[]
with pdfplumber.open(pdf) as d:
 for i,page in enumerate(d.pages):
  for word in page.extract_words():
   if word['x0']<40 or word['x1']>page.width-40 or word['top']<12 or word['bottom']>page.height-18:
    bad.append((i+1,word['text'],word['x0'],word['x1'],word['top'],word['bottom']))
assert not bad,bad[:10]
# Verify every relative Markdown link points to a real local document.
for target in re.findall(r'\]\(([^)]+)\)',source):
 if not target.startswith(('https:','http:','#')):assert (root/'docs'/target).exists(),target
out=root/'tmp/pdfs/domain-guide';doc=pdfium.PdfDocument(str(pdf))
for i,page in enumerate(doc):page.render(scale=1.5).to_pil().save(out/f'page-{i+1:02}.png')
thumb_w,thumb_h=240,340;sheet=Image.new('RGB',(1200,5*362),'#e6e9ef');draw=ImageDraw.Draw(sheet)
for i in range(len(doc)):
 im=Image.open(out/f'page-{i+1:02}.png');im.thumbnail((230,332));x=i%5*240+5;y=i//5*362;sheet.paste(im,(x,y));draw.text((x+5,y+340),str(i+1),fill='black')
sheet.save(out/'final-contact-sheet.jpg',quality=91)
summary={'pdf':str(pdf),'pages':len(reader.pages),'bookmarks':len(reader.outline),'hyperlinks':sum(len(p.get('/Annots',[])) for p in reader.pages),'out_of_bounds_words':len(bad),'chapter_breaks':source.count('<!-- pagebreak -->'),'relative_links':'all valid','size_kb':round(pdf.stat().st_size/1024,1)}
(out/'validation.json').write_text(json.dumps(summary,indent=2));print(json.dumps(summary,indent=2))
