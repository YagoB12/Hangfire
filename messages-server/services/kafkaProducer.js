// services/kafkaProducer.js
import { Kafka } from "kafkajs";
import dotenv from "dotenv";
dotenv.config();

export class KafkaProducer {
  constructor(broker, topic) {
    this.kafka = new Kafka({
      clientId: "messages-server",
      brokers: [broker],
    });
    this.producer = this.kafka.producer();
    this.topic = topic;
    this.connect();
  }

  async connect() {
    try {
      await this.producer.connect();
      console.log("Conectado a Kafka:", this.topic);
    } catch (err) {
      console.error("Error conectando a Kafka:", err.message);
    }
  }

  async sendLog(message) {
    try {
      await this.producer.send({
        topic: this.topic,
        messages: [{ value: JSON.stringify(message) }],
      });
      console.log("Log enviado a Kafka:", message.event);
    } catch (err) {
      console.error("Error enviando log a Kafka:", err.message);
    }
  }
}
