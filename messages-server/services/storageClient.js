// services/storageClient.js
import axios from "axios";
import fs from "fs";
import path from "path";
import dotenv from "dotenv";
dotenv.config();

export class StorageClient {
  constructor() {
    this.baseUrl = process.env.STORAGE_SERVER_URL;
  }

  async downloadPdf(correlationId) {
    try {
      const url = `${this.baseUrl}/download/by-correlation/${correlationId}`;
      const response = await axios.get(url, { responseType: "arraybuffer" });

      // Guardar temporalmente en carpeta local
      const folder = path.join(process.cwd(), "downloads");
      if (!fs.existsSync(folder)) fs.mkdirSync(folder);

      const filePath = path.join(folder, `${correlationId}.pdf`);
      fs.writeFileSync(filePath, response.data);
      console.log(`PDF descargado: ${filePath}`);
      return filePath;
    } catch (err) {
      console.error("Error descargando PDF:", err.message);
      throw err;
    }
  }
}
