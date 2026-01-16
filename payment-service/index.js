const express = require("express");
const mongoose = require("mongoose");
const cors = require("cors"); // IMPORT CORS
const app = express();
const PORT = 3000;

// --- CONFIGURATION CORS (WAJIB) ---
app.use(cors()); // Izinkan semua origin (Flutter Web)
app.use(express.json());

// URL Koneksi MongoDB (Service name: mongo-db)
const MONGO_URI = "mongodb://mongo-db:27017/bioskop_payment";

const connectWithRetry = () => {
  mongoose
    .connect(MONGO_URI)
    .then(() => console.log("Connected to MongoDB Successfully!"))
    .catch((err) => {
      console.error("Failed to connect to MongoDB, retrying in 5s...", err);
      setTimeout(connectWithRetry, 5000);
    });
};

connectWithRetry();

// Schema
const PaymentSchema = new mongoose.Schema({
  order_id: String,
  amount: Number,
  movie_title: String, // Tambahan info film
  seat_number: String, // Tambahan info kursi
  payer_name: String, // Tambahan nama pembayar
  status: { type: String, default: "Success" },
  timestamp: { type: Date, default: Date.now },
});

const Payment = mongoose.model("Payment", PaymentSchema);

// --- ROUTES ---

app.get("/", (req, res) => {
  res.json({
    service: "Payment Service",
    language: "Node.js",
    database: "MongoDB",
  });
});

// POST: Bayar
app.post("/pay", async (req, res) => {
  try {
    // Kita terima data lebih lengkap dari Flutter
    const { order_id, amount, movie_title, seat_number, payer_name } = req.body;

    const newPayment = new Payment({
      order_id,
      amount,
      movie_title,
      seat_number,
      payer_name,
      status: "Success",
    });

    await newPayment.save();
    console.log(`Payment saved for ${payer_name}`);

    res.json({ message: "Payment processed", data: newPayment });
  } catch (error) {
    res.status(500).json({ error: error.message });
  }
});

// GET: History
app.get("/history", async (req, res) => {
  try {
    const history = await Payment.find().sort({ timestamp: -1 }); // Urutkan terbaru
    res.json(history);
  } catch (error) {
    res.status(500).json({ error: error.message });
  }
});

app.listen(PORT, () => {
  console.log(`Payment service running on port ${PORT}`);
});
