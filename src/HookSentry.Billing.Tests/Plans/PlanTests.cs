using HookSentry.Billing.Plans;

namespace HookSentry.Billing.Tests.Plans;

public class PlanTests
{
    private const string ValidName = "free";
    private const int ValidMaxUsers = -1;
    private const int ValidMaxDestinations = -1;
    private const int ValidMaxEventsPerMonth = 1_000;
    private const int ValidRetentionDays = 7;

    private static Plan CriarPlano() =>
        new(ValidName, ValidMaxUsers, ValidMaxDestinations, ValidMaxEventsPerMonth, ValidRetentionDays);

    public class Construtor
    {
        [Fact]
        public void Deve_Gerar_Id_Nao_Vazio()
        {
            var plan = CriarPlano();
            Assert.NotEqual(Guid.Empty, plan.Id);
        }

        [Fact]
        public void Dois_Planos_Devem_Ter_Ids_Diferentes()
        {
            var a = CriarPlano();
            var b = CriarPlano();
            Assert.NotEqual(a.Id, b.Id);
        }

        [Fact]
        public void Deve_Normalizar_Name_Para_Lowercase()
        {
            var plan = new Plan("FREE", ValidMaxUsers, ValidMaxDestinations, ValidMaxEventsPerMonth, ValidRetentionDays);
            Assert.Equal("free", plan.Name);
        }

        [Fact]
        public void Deve_Atribuir_MaxUsers_Informado()
        {
            var plan = CriarPlano();
            Assert.Equal(ValidMaxUsers, plan.MaxUsers);
        }

        [Fact]
        public void Deve_Atribuir_MaxDestinations_Informado()
        {
            var plan = CriarPlano();
            Assert.Equal(ValidMaxDestinations, plan.MaxDestinations);
        }

        [Fact]
        public void Deve_Atribuir_MaxEventsPerMonth_Informado()
        {
            var plan = CriarPlano();
            Assert.Equal(ValidMaxEventsPerMonth, plan.MaxEventsPerMonth);
        }

        [Fact]
        public void Deve_Atribuir_RetentionDays_Informado()
        {
            var plan = CriarPlano();
            Assert.Equal(ValidRetentionDays, plan.RetentionDays);
        }

        [Fact]
        public void Deve_Aceitar_Limites_Ilimitados()
        {
            var plan = new Plan("free", -1, -1, -1, ValidRetentionDays);
            Assert.Equal(-1, plan.MaxUsers);
            Assert.Equal(-1, plan.MaxDestinations);
            Assert.Equal(-1, plan.MaxEventsPerMonth);
        }

        [Fact]
        public void CreatedAt_Deve_Ser_Definido_Como_UtcNow()
        {
            var antes = DateTimeOffset.UtcNow;
            var plan = CriarPlano();
            Assert.True(plan.CreatedAt >= antes);
        }

        [Fact]
        public void UpdatedAt_Deve_Ser_Definido_Como_UtcNow()
        {
            var antes = DateTimeOffset.UtcNow;
            var plan = CriarPlano();
            Assert.True(plan.UpdatedAt >= antes);
        }

        [Fact]
        public void CreatedAt_E_UpdatedAt_Devem_Ser_Iguais_Na_Criacao()
        {
            var plan = CriarPlano();
            Assert.Equal(plan.CreatedAt, plan.UpdatedAt);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Deve_Lancar_Excecao_Quando_Name_For_Invalido(string? name)
        {
            Assert.Throws<ArgumentException>(() =>
                new Plan(name!, ValidMaxUsers, ValidMaxDestinations, ValidMaxEventsPerMonth, ValidRetentionDays));
        }

        [Theory]
        [InlineData(-2)]
        [InlineData(-100)]
        public void Deve_Lancar_Excecao_Quando_MaxUsers_For_Invalido(int maxUsers)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new Plan(ValidName, maxUsers, ValidMaxDestinations, ValidMaxEventsPerMonth, ValidRetentionDays));
        }

        [Theory]
        [InlineData(-2)]
        [InlineData(-100)]
        public void Deve_Lancar_Excecao_Quando_MaxDestinations_For_Invalido(int maxDestinations)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new Plan(ValidName, ValidMaxUsers, maxDestinations, ValidMaxEventsPerMonth, ValidRetentionDays));
        }

        [Theory]
        [InlineData(-2)]
        [InlineData(-100)]
        public void Deve_Lancar_Excecao_Quando_MaxEventsPerMonth_For_Invalido(int maxEventsPerMonth)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new Plan(ValidName, ValidMaxUsers, ValidMaxDestinations, maxEventsPerMonth, ValidRetentionDays));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-10)]
        public void Deve_Lancar_Excecao_Quando_RetentionDays_For_Invalido(int retentionDays)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new Plan(ValidName, ValidMaxUsers, ValidMaxDestinations, ValidMaxEventsPerMonth, retentionDays));
        }
    }
}
