# Gunakan Node.js versi ringan
FROM node:18-alpine

# Set folder kerja di dalam container
WORKDIR /app

# Copy file package.json dulu biar cache Docker optimal
COPY package.json ./

# Install library (express & mongoose)
RUN npm install

# Copy sisa kode (index.js)
COPY . .

# Expose port internal Node.js
EXPOSE 3000

# Jalankan aplikasi
CMD ["npm", "start"]