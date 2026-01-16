const express = require("express");
const mongoose = require("mongoose");
const cors = require("cors");
const bcrypt = require("bcryptjs");
const jwt = require("jsonwebtoken");

const app = express();
const PORT = 3001;
const SECRET_KEY = "rahasia_negara_yang_mulia";

app.use(cors());
app.use(express.json());

// --- KONEKSI DATABASE ---
const MONGO_URI = "mongodb://mongo-db:27017/bioskop_auth";

const connectWithRetry = () => {
  mongoose
    .connect(MONGO_URI)
    .then(() => console.log("Auth Service Connected to MongoDB"))
    .catch((err) => {
      console.error("MongoDB Auth error, retrying...", err);
      setTimeout(connectWithRetry, 5000);
    });
};
connectWithRetry();

// --- SCHEMA USER (UPDATE: Tambah Role) ---
const UserSchema = new mongoose.Schema({
  username: { type: String, required: true, unique: true },
  password: { type: String, required: true },
  fullName: String,
  role: {
    type: String,
    enum: ["admin", "pengunjung"], // Hanya boleh 2 ini
    default: "pengunjung",
  },
});

const User = mongoose.model("User", UserSchema);

app.get("/", (req, res) =>
  res.json({ service: "Auth Service", status: "Active" }),
);

// 1. REGISTER (UPDATE: Terima Role dari Flutter)
app.post("/register", async (req, res) => {
  try {
    // Ambil role dari body, default ke 'pengunjung' jika kosong
    const { username, password, fullName, role } = req.body;

    const existingUser = await User.findOne({ username });
    if (existingUser)
      return res.status(400).json({ message: "Username sudah dipakai" });

    const hashedPassword = await bcrypt.hash(password, 10);

    const newUser = new User({
      username,
      password: hashedPassword,
      fullName,
      role: role || "pengunjung", // Simpan Role
    });

    await newUser.save();
    res.json({ message: `Registrasi ${role} Berhasil!` });
  } catch (err) {
    res.status(500).json({ error: err.message });
  }
});

// 2. LOGIN (UPDATE: Kirim Role balik ke Flutter)
app.post("/login", async (req, res) => {
  try {
    const { username, password } = req.body;

    const user = await User.findOne({ username });
    if (!user) return res.status(404).json({ message: "User tidak ditemukan" });

    const isMatch = await bcrypt.compare(password, user.password);
    if (!isMatch) return res.status(400).json({ message: "Password salah!" });

    // Masukkan role ke dalam Token
    const token = jwt.sign(
      { id: user._id, username: user.username, role: user.role },
      SECRET_KEY,
      { expiresIn: "1h" },
    );

    res.json({
      message: "Login Sukses",
      token: token,
      username: user.username,
      fullName: user.fullName,
      role: user.role, // <-- PENTING: Kirim role ke Frontend
    });
  } catch (err) {
    res.status(500).json({ error: err.message });
  }
});

app.listen(PORT, () => {
  console.log(`Auth Service running on port ${PORT}`);
});
