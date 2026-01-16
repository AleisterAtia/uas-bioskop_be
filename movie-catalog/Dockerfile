# Gunakan image PHP dengan Apache
FROM php:7.4-apache

# Install ekstensi mysqli untuk koneksi ke MySQL
RUN docker-php-ext-install mysqli

# Salin source code ke dalam folder web server container
COPY src/ /var/www/html/

# Expose port 80 (internal container)
EXPOSE 80