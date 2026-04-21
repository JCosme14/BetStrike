FROM nginx:alpine

# Copy the HTML and JS files to the nginx directory
COPY frontend/index.html /usr/share/nginx/html/index.html
COPY frontend/app.js /usr/share/nginx/html/app.js

# Copy nginx config
COPY frontend/nginx.conf /etc/nginx/nginx.conf

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]
