using System;
using System.Linq;
using Godot;

namespace DamselsGambit.Util;

public static class SurfaceToolExtensions
{
    public static void AddVertex(this SurfaceTool self, Vector3 vertex, Vector2 uv) { self.SetUV(uv); self.AddVertex(vertex); }
    
    public static void AddVertex2D(this SurfaceTool self, Vector2 vertex) { self.AddVertex(new Vector3(vertex.X, vertex.Y, 0f)); }
    public static void AddVertex2D(this SurfaceTool self, Vector2 vertex, Vector2 uv) { self.SetUV(uv); self.AddVertex2D(vertex); }

    public static void AddTri(this SurfaceTool self, Vector3 a, Vector3 b, Vector3 c, Vector2 aUV, Vector2 bUV, Vector2 cUV) {
        self.AddVertex(a, aUV); self.AddVertex(b, bUV); self.AddVertex(c, cUV);
        switch (self.GetPrimitiveType()) {
            case Mesh.PrimitiveType.Lines: {
                self.AddVertex(a, aUV); self.AddVertex(b, bUV);
                self.AddVertex(b, bUV); self.AddVertex(c, cUV);
                self.AddVertex(c, cUV); self.AddVertex(a, aUV);
            } break;
            case Mesh.PrimitiveType.LineStrip: { self.AddVertex(a, aUV); self.AddVertex(b, bUV); self.AddVertex(c, cUV); self.AddVertex(a, aUV); } break;
            default: {
                self.AddVertex(a, aUV); self.AddVertex(b, bUV); self.AddVertex(c, cUV);
            } break;
        }
    }

    public static void AddQuad(this SurfaceTool self, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector2 aUV, Vector2 bUV, Vector2 cUV, Vector2 dUV) {
        switch (self.GetPrimitiveType()) {
            case Mesh.PrimitiveType.Triangles: {
                self.AddTri(a, b, c, aUV, bUV, cUV);
                self.AddTri(a, c, d, aUV, cUV, dUV);
            } break;
            case Mesh.PrimitiveType.Lines: {
                self.AddVertex(a, aUV); self.AddVertex(b, bUV);
                self.AddVertex(b, bUV); self.AddVertex(c, cUV);
                self.AddVertex(c, cUV); self.AddVertex(d, dUV);
                self.AddVertex(d, dUV); self.AddVertex(a, aUV);
            } break;
            case Mesh.PrimitiveType.LineStrip: { self.AddVertex(a, aUV); self.AddVertex(b, bUV); self.AddVertex(c, cUV); self.AddVertex(d, dUV); self.AddVertex(a, aUV); } break;
            default: {
                self.AddVertex(a, aUV); self.AddVertex(b, bUV); self.AddVertex(c, cUV); self.AddVertex(d, dUV); 
            } break;
        }
    }

    public static void AddTri2D(this SurfaceTool self, Vector2 a, Vector2 b, Vector2 c, Vector2 aUV, Vector2 bUV, Vector2 cUV)
        => self.AddTri(new(a.X, a.Y, 0f), new(b.X, b.Y, 0f), new(c.X, c.Y, 0f), aUV, bUV, cUV);
    public static void AddQuad2D(this SurfaceTool self, Vector2 a, Vector2 b, Vector2 c, Vector2 d, Vector2 aUV, Vector2 bUV, Vector2 cUV, Vector2 dUV)
        => self.AddQuad(new(a.X, a.Y, 0f), new(b.X, b.Y, 0f), new(c.X, c.Y, 0f), new(d.X, d.Y, 0f), aUV, bUV, cUV, dUV);
}