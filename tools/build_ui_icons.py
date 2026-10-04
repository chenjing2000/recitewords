"""Convert the supplied SVG paths/rectangles to WPF drawings and PNG to an EXE icon."""
from pathlib import Path
import xml.etree.ElementTree as ET
from xml.sax.saxutils import quoteattr
from PIL import Image

root = Path(__file__).resolve().parents[1]
ui = root / "ReciteWords/ui"
names = {
    "OpenFolder": "icon_open_folder.svg", "PreviousWord": "icon_prev_word.svg",
    "NextWord": "icon_next_word.svg", "Explanation": "icon_explanation.svg",
}
lines = ['<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">',
         '  <!-- Generated from ui/*.svg by tools/build_ui_icons.py. -->']
for name, filename in names.items():
    svg = ET.parse(ui / filename).getroot()
    lines += [f'  <DrawingImage x:Key="{name}Icon"><DrawingImage.Drawing><DrawingGroup>',
              '    <GeometryDrawing Brush="Transparent" Geometry="M0,0 H24 V24 H0 Z" />']
    for element in svg:
        tag = element.tag.split('}')[-1]
        fill = element.get('fill', svg.get('fill', 'black'))
        brush = '{x:Null}' if fill == 'none' else fill
        lines.append(f'    <DrawingGroup Opacity={quoteattr(element.get("opacity", "1"))}><GeometryDrawing Brush={quoteattr(brush)}>')
        if tag == 'path':
            geometry = f'<PathGeometry Figures={quoteattr(element.attrib["d"])} />'
        elif tag == 'rect':
            rectangle = ','.join(element.attrib[a] for a in ('x', 'y', 'width', 'height'))
            radius = element.get('rx', '0')
            geometry = f'<RectangleGeometry Rect={quoteattr(rectangle)} RadiusX={quoteattr(radius)} RadiusY={quoteattr(element.get("ry", radius))} />'
        else:
            raise ValueError(f'Unsupported element: {tag}')
        lines.append(f'      <GeometryDrawing.Geometry>{geometry}</GeometryDrawing.Geometry>')
        if 'stroke' in element.attrib:
            cap = 'Round' if element.get('stroke-linecap') == 'round' else 'Flat'
            join = 'Round' if element.get('stroke-linejoin') == 'round' else 'Miter'
            lines.append(f'      <GeometryDrawing.Pen><Pen Brush={quoteattr(element.attrib["stroke"])} Thickness={quoteattr(element.get("stroke-width", "1"))} StartLineCap="{cap}" EndLineCap="{cap}" LineJoin="{join}" /></GeometryDrawing.Pen>')
        lines.append('    </GeometryDrawing></DrawingGroup>')
    lines.append('  </DrawingGroup></DrawingImage.Drawing></DrawingImage>')
lines.append('</ResourceDictionary>')
(root / 'ReciteWords/Resources/Icons.xaml').write_text('\n'.join(lines) + '\n', encoding='utf-8')
with Image.open(ui / 'mainicon.png') as image:
    image.save(ui / 'mainicon.ico', sizes=[(s, s) for s in (16, 24, 32, 48, 64, 128, 256)])
