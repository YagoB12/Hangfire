// app.js
import express from "express";
import dotenv from "dotenv";
import { KafkaProducer } from "./services/kafkaProducer.js";
import { MessagingService } from "./services/messagingService.js";

dotenv.config();

const app = express();
app.use(express.json());

const kafka = new KafkaProducer(process.env.KAFKA_BROKER, process.env.KAFKA_TOPIC);
const messaging = new MessagingService(kafka);

app.post("/api/messaging/send", async (req, res) => {
  try {
    const { correlationId, platform, recipient, message } = req.body;
    if (!correlationId || !platform) {
      return res.status(400).json({ error: "Faltan parámetros obligatorios" });
    }

    const result = await messaging.sendMessage({
      correlationId,
      platform,
      recipient,
      message,
    });

    res.json({ status: "ok", result });
  } catch (err) {
    console.error(" Error en /api/messaging/send:", err.message);
    res.status(500).json({ error: err.message });
  }
});

const PORT = process.env.PORT || 8004;
app.listen(PORT, () => console.log(`Messages Server activo en http://localhost:${PORT}`));
