import http from "node:http";
import { createReadStream, promises as fs } from "node:fs";
import path from "node:path";

const root = path.resolve(process.argv[2] ?? "WebGLBuild");
const port = Number.parseInt(process.argv[3] ?? "8000", 10);

const mimeTypes = new Map([
  [".html", "text/html; charset=utf-8"],
  [".css", "text/css; charset=utf-8"],
  [".js", "text/javascript; charset=utf-8"],
  [".json", "application/json; charset=utf-8"],
  [".wasm", "application/wasm"],
  [".data", "application/octet-stream"],
  [".ogg", "audio/ogg"],
  [".mp3", "audio/mpeg"],
  [".wav", "audio/wav"],
  [".glb", "model/gltf-binary"],
  [".png", "image/png"],
  [".ico", "image/x-icon"],
]);

function resolveRequestPath(url) {
  const pathname = decodeURIComponent(new URL(url, "http://localhost").pathname);
  const relative = pathname === "/" ? "index.html" : pathname.replace(/^\/+/, "");
  const candidate = path.resolve(root, relative);
  if (candidate !== root && !candidate.startsWith(`${root}${path.sep}`)) return null;
  return candidate;
}

const server = http.createServer(async (request, response) => {
  try {
    let filePath = resolveRequestPath(request.url ?? "/");
    if (filePath === null) {
      response.writeHead(403).end("Forbidden");
      return;
    }

    let stat = await fs.stat(filePath);
    if (stat.isDirectory()) {
      filePath = path.join(filePath, "index.html");
      stat = await fs.stat(filePath);
    }
    if (!stat.isFile()) throw new Error("Not a file");

    const compressed = filePath.endsWith(".gz");
    const contentPath = compressed ? filePath.slice(0, -3) : filePath;
    const mimeType = mimeTypes.get(path.extname(contentPath).toLowerCase()) ?? "application/octet-stream";
    const headers = {
      "Content-Type": mimeType,
      "Content-Length": stat.size,
      "Cache-Control": "no-cache",
    };
    if (compressed) headers["Content-Encoding"] = "gzip";

    response.writeHead(200, headers);
    if (request.method === "HEAD") response.end();
    else createReadStream(filePath).pipe(response);
  } catch {
    response.writeHead(404, { "Content-Type": "text/plain; charset=utf-8" }).end("Not found");
  }
});

server.listen(port, "127.0.0.1", () => {
  console.log(`WebGL disponible en http://127.0.0.1:${port}/`);
  console.log(`Raíz: ${root}`);
});
