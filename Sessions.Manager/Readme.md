# Testing in docker
```
docker compose -f ./docker_compose.yaml build
docker compose -f ./docker_compose.yaml up
```

To run only redis for local development
```
docker compose -f ./docker_compose.yaml up redis
```