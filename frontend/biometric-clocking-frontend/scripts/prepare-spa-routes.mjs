import { mkdir, copyFile } from "node:fs/promises";
import path from "node:path";
import { fileURLToPath } from "node:url";

const projectRoot = path.dirname(fileURLToPath(import.meta.url));
const distDirectory = path.resolve(projectRoot, "..", "dist");
const entryDocument = path.join(distDirectory, "index.html");
const routes = ["kiosk", "admin", "dashboard", "onboard", "hr"];

for (const route of routes) {
  const routeDirectory = path.join(distDirectory, route);
  await mkdir(routeDirectory, { recursive: true });
  await copyFile(entryDocument, path.join(routeDirectory, "index.html"));
}

console.log(`Prepared direct SPA entry points: ${routes.join(", ")}`);
