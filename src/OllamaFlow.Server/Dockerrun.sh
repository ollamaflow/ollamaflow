if [ -z "${IMG_TAG}" ]; then
  IMG_TAG='v1.0.0'
fi

echo Using image tag $IMG_TAG

if [ ! -f "ollamaflow.json" ]
then
  echo Configuration file ollamaflow.json not found.
  exit
fi

# Items that require persistence
#   ollamaflow.json
#   ollamaflow.db
#   logs/

# Argument order matters!

docker run \
  -p 43411:43411 \
  -t \
  -i \
  -e "TERM=xterm-256color" \
  -v ./ollamaflow.json:/app/ollamaflow.json \
  -v ./ollamaflow.db:/app/ollamaflow.db \
  -v ./logs/:/app/logs/ \
  jchristn/ollamaflow:$IMG_TAG
