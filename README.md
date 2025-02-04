# betssy-becky-qy-testy

Test task for BEPAMQA

## Howto

### Build Docker image

Run this command from the directory where there is the solution file.

```
docker build -f src/Betsson.OnlineWallets.Web/Dockerfile -t betsson-online-wallet .
```

### Run Docker container

```
docker run -it --rm -p 8080:8080 betsson-online-wallet
```

### Open Swagger

```
http://localhost:8080/swagger/index.html
```
