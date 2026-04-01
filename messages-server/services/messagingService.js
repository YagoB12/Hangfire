// services/messagingService.js
import { Client, GatewayIntentBits } from "discord.js";
import { StorageClient } from "./storageClient.js";
import dotenv from "dotenv";
import fs from "fs";
dotenv.config();

export class MessagingService {
  constructor(kafkaProducer) {
    this.kafka = kafkaProducer;
    this.storage = new StorageClient();

    this.client = new Client({
      intents: [GatewayIntentBits.Guilds, GatewayIntentBits.GuildMessages],
    });

    this.channelId = process.env.DISCORD_CHANNEL_ID;

    // Conectar bot
    this.client.login(process.env.DISCORD_BOT_TOKEN).then(() => {
      console.log(" Bot de Discord conectado correctamente");
    });
  }

  async sendMessage({ correlationId, platform, recipient, message }) {
    if (platform !== "discord") {
      throw new Error(`Plataforma no soportada: ${platform}`);
    }

    try {
      // 1️1 Descargar PDF desde el StorageServer
      const filePath = await this.storage.downloadPdf(correlationId);

      // 2️ Enviar PDF al canal de Discord
      const channel = await this.client.channels.fetch(this.channelId);
      await channel.send({
        content: message || ` Archivo generado (CID: ${correlationId})`,
        files: [filePath],
      });

      console.log(` Archivo enviado a Discord (${this.channelId})`);

      // 3️ Log a Kafka
      await this.kafka.sendLog({
        event: "message_sent",
        platform,
        correlationId,
        channelId: this.channelId,
        timestamp: new Date().toISOString(),
      });

      // Eliminar el archivo temporal
      fs.unlinkSync(filePath);

      return { success: true, sentTo: "Discord Channel", correlationId };
    } catch (err) {
      console.error(" Error enviando mensaje a Discord:", err.message);
      await this.kafka.sendLog({
        event: "error",
        platform,
        correlationId,
        error: err.message,
        timestamp: new Date().toISOString(),
      });
      throw err;
    }
  }
}
