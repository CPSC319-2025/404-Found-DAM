if [ ! -f "./frontend/.env" ]; then
  echo "Missing frontend/.env"
  exit 1
fi

echo "Installing dependencies..."
cd frontend
npm install

echo "Starting frontend..."
npm run dev
