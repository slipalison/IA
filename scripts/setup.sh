
#!/bin/bash
# scripts/setup.sh




echo "🚀 Configurando DevOpsAI Infrastructure (CPU Mode)..."

# Verificar se Docker está rodando
if ! docker info > /dev/null 2>&1; then
    echo "❌ Docker não está rodando. Inicie o Docker primeiro."
    exit 1
fi

# Criar diretórios necessários
echo "📁 Criando diretórios..."
mkdir -p infrastructure/{chroma,ollama,grafana/dashboards,postgres}

# Parar containers existentes se houver
echo "🛑 Parando containers existentes..."
docker compose down 2>/dev/null || true

# Subir a infraestrutura
echo "📦 Iniciando containers..."
docker compose up -d

# Aguardar serviços ficarem prontos
echo "⏳ Aguardando serviços (60s)..."
sleep 60

# Verificar se containers estão rodando
echo "🔍 Verificando status dos containers..."
docker compose ps

# Baixar modelos do Ollama (versões CPU-friendly)
echo "🤖 Baixando modelos de IA..."

echo "  📥 Baixando Llama2 7B (modelo geral)..."
docker exec devops-ollama ollama pull llama2:7b

echo "  📥 Baixando CodeLlama 7B (para código)..."
docker exec devops-ollama ollama pull codellama:7b

docker exec devops-ollama ollama pull mxbai-embed-large


# Verificar saúde dos serviços
echo "🔍 Verificando saúde dos serviços..."

# Usar endpoints que funcionam
curl -s http://localhost:8000/api/v1/heartbeat > /dev/null 2>&1 && echo "✅ Chroma DB" || echo "❌ Chroma DB"
curl -f http://localhost:11434/api/tags > /dev/null 2>&1 && echo "✅ Ollama API" || echo "❌ Ollama API"
curl -f http://localhost:3000/api/health > /dev/null 2>&1 && echo "✅ Grafana" || echo "❌ Grafana"

echo ""
echo "✅ Setup concluído!"
echo "🌐 Acessos disponíveis:"
echo "  - Chroma DB: http://localhost:8000/docs"
echo "  - Ollama API: http://localhost:11434"
echo "  - Grafana: http://localhost:3000 (admin/admin123)"
echo "  - PostgreSQL: localhost:5432"
echo ""
echo "🧪 Para testar o modelo:"
echo "  docker exec devops-ollama ollama run llama2:7b 'Hello!'"
echo ""
echo "🔍 Para verificar saúde dos serviços:"
echo "  ./scripts/service_health_check.sh"