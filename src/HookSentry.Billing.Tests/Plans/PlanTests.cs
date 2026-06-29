using HookSentry.Billing.Plans;

namespace HookSentry.Billing.Tests.Plans;

public class PlanTests
{
    private const string ValidName = "starter";
    private const string ValidStripePriceId = "price_abc123";
    private const decimal ValidPrice = 29m;
    private const int ValidMaxUsers = 10;
    private const int ValidMaxDestinations = 20;
    private const int ValidMaxEventsPerMonth = 100_000;
    private const PlanFeature ValidFeatures = PlanFeature.Webhooks | PlanFeature.RetryPolicy;

    public class Construtor
    {
        [Fact]
        public void Deve_Gerar_Id_Nao_Vazio()
        {
            var plan = new Plan(ValidName, ValidStripePriceId, ValidPrice, ValidMaxUsers, ValidMaxDestinations, ValidMaxEventsPerMonth, ValidFeatures);
            Assert.NotEqual(Guid.Empty, plan.Id);
        }

        [Fact]
        public void Dois_Planos_Devem_Ter_Ids_Diferentes()
        {
            var a = new Plan(ValidName, ValidStripePriceId, ValidPrice, ValidMaxUsers, ValidMaxDestinations, ValidMaxEventsPerMonth, ValidFeatures);
            var b = new Plan(ValidName, ValidStripePriceId, ValidPrice, ValidMaxUsers, ValidMaxDestinations, ValidMaxEventsPerMonth, ValidFeatures);
            Assert.NotEqual(a.Id, b.Id);
        }

        [Fact]
        public void Deve_Normalizar_Name_Para_Lowercase()
        {
            var plan = new Plan("STARTER", ValidStripePriceId, ValidPrice, ValidMaxUsers, ValidMaxDestinations, ValidMaxEventsPerMonth, ValidFeatures);
            Assert.Equal("starter", plan.Name);
        }

        [Fact]
        public void Deve_Aceitar_StripePriceId_Nulo()
        {
            var plan = new Plan("enterprise", null, 0m, -1, -1, -1, PlanFeature.Webhooks);
            Assert.Null(plan.StripePriceId);
        }

        [Fact]
        public void Deve_Atribuir_PriceMonthly_Informado()
        {
            var plan = new Plan(ValidName, ValidStripePriceId, 99m, ValidMaxUsers, ValidMaxDestinations, ValidMaxEventsPerMonth, ValidFeatures);
            Assert.Equal(99m, plan.PriceMonthly);
        }

        [Fact]
        public void Deve_Aceitar_Limites_Ilimitados()
        {
            var plan = new Plan("enterprise", null, 0m, -1, -1, -1, PlanFeature.Webhooks);
            Assert.Equal(-1, plan.MaxUsers);
            Assert.Equal(-1, plan.MaxDestinations);
            Assert.Equal(-1, plan.MaxEventsPerMonth);
        }

        [Fact]
        public void Deve_Atribuir_Features_Informadas()
        {
            var plan = new Plan(ValidName, ValidStripePriceId, ValidPrice, ValidMaxUsers, ValidMaxDestinations, ValidMaxEventsPerMonth, ValidFeatures);
            Assert.Equal(ValidFeatures, plan.Features);
        }

        [Fact]
        public void IsActive_Deve_Ser_True_Na_Criacao()
        {
            var plan = new Plan(ValidName, ValidStripePriceId, ValidPrice, ValidMaxUsers, ValidMaxDestinations, ValidMaxEventsPerMonth, ValidFeatures);
            Assert.True(plan.IsActive);
        }

        [Fact]
        public void CreatedAt_Deve_Ser_Definido_Como_UtcNow()
        {
            var antes = DateTimeOffset.UtcNow;
            var plan = new Plan(ValidName, ValidStripePriceId, ValidPrice, ValidMaxUsers, ValidMaxDestinations, ValidMaxEventsPerMonth, ValidFeatures);
            Assert.True(plan.CreatedAt >= antes);
        }

        [Fact]
        public void UpdatedAt_Deve_Ser_Definido_Como_UtcNow()
        {
            var antes = DateTimeOffset.UtcNow;
            var plan = new Plan(ValidName, ValidStripePriceId, ValidPrice, ValidMaxUsers, ValidMaxDestinations, ValidMaxEventsPerMonth, ValidFeatures);
            Assert.True(plan.UpdatedAt >= antes);
        }

        [Fact]
        public void CreatedAt_E_UpdatedAt_Devem_Ser_Iguais_Na_Criacao()
        {
            var plan = new Plan(ValidName, ValidStripePriceId, ValidPrice, ValidMaxUsers, ValidMaxDestinations, ValidMaxEventsPerMonth, ValidFeatures);
            Assert.Equal(plan.CreatedAt, plan.UpdatedAt);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Deve_Lancar_Excecao_Quando_Name_For_Invalido(string? name)
        {
            Assert.Throws<ArgumentException>(() =>
                new Plan(name!, ValidStripePriceId, ValidPrice, ValidMaxUsers, ValidMaxDestinations, ValidMaxEventsPerMonth, ValidFeatures));
        }

        [Fact]
        public void Deve_Lancar_Excecao_Quando_StripePriceId_For_String_Vazia()
        {
            Assert.Throws<ArgumentException>(() =>
                new Plan(ValidName, "", ValidPrice, ValidMaxUsers, ValidMaxDestinations, ValidMaxEventsPerMonth, ValidFeatures));
        }

        [Fact]
        public void Deve_Lancar_Excecao_Quando_PriceMonthly_For_Negativo()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new Plan(ValidName, ValidStripePriceId, -1m, ValidMaxUsers, ValidMaxDestinations, ValidMaxEventsPerMonth, ValidFeatures));
        }

        [Theory]
        [InlineData(-2)]
        [InlineData(-100)]
        public void Deve_Lancar_Excecao_Quando_MaxUsers_For_Invalido(int maxUsers)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new Plan(ValidName, ValidStripePriceId, ValidPrice, maxUsers, ValidMaxDestinations, ValidMaxEventsPerMonth, ValidFeatures));
        }

        [Theory]
        [InlineData(-2)]
        [InlineData(-100)]
        public void Deve_Lancar_Excecao_Quando_MaxDestinations_For_Invalido(int maxDestinations)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new Plan(ValidName, ValidStripePriceId, ValidPrice, ValidMaxUsers, maxDestinations, ValidMaxEventsPerMonth, ValidFeatures));
        }

        [Theory]
        [InlineData(-2)]
        [InlineData(-100)]
        public void Deve_Lancar_Excecao_Quando_MaxEventsPerMonth_For_Invalido(int maxEventsPerMonth)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new Plan(ValidName, ValidStripePriceId, ValidPrice, ValidMaxUsers, ValidMaxDestinations, maxEventsPerMonth, ValidFeatures));
        }
    }
}
