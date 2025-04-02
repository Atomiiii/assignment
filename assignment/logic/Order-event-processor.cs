using Npgsql;

namespace EventProcessing {
    public class EventProcessor {
        private static readonly string connectionString = "Host=postgres_db;Port=5432;Database=orders_db;Username=postgres;Password=postgres";
        public async Task ProcessOrderEventAsync(string id, string product, decimal total, string currency) {
            try {
                using (var connection = new NpgsqlConnection(connectionString)) {
                    await connection.OpenAsync();

                    // Check if payment exists
                    string query = "SELECT amount FROM payments WHERE id = @id";
                    using (var command = new NpgsqlCommand(query, connection)) {
                        command.Parameters.AddWithValue("id", id);
                        var result = await command.ExecuteScalarAsync();

                        decimal paid = result == null ? 0 : Convert.ToDecimal(result);
                        bool isPaid = paid >= total;

                        // Insert or update order
                        string insertOrderQuery = "INSERT INTO orders (id, product, total, currency, isPaid) VALUES (@id, @product, @total, @currency, @isPaid)";
                        using (var orderCommand = new NpgsqlCommand(insertOrderQuery, connection)) {
                            orderCommand.Parameters.AddWithValue("id", id);
                            orderCommand.Parameters.AddWithValue("product", product);
                            orderCommand.Parameters.AddWithValue("total", total);
                            orderCommand.Parameters.AddWithValue("currency", currency);
                            orderCommand.Parameters.AddWithValue("isPaid", isPaid);
                            await orderCommand.ExecuteNonQueryAsync();
                        }
                        // If paid, print out
                        if (isPaid) {
                            Console.WriteLine($"Order {id} for {product} has been fully paid.");
                        }
                    }
                }
            } catch (NpgsqlException e) {
                Console.WriteLine($"Database error processing order: {e.Message}");
            } catch (Exception e) {
                Console.WriteLine($"Unexpected error: {e}");
            }
        }

        public async Task ProcessPaymentEventAsync(string id, decimal paid) {
            try {
                using (var connection = new NpgsqlConnection(connectionString)) {
                    await connection.OpenAsync();

                    // Check if payment exists
                    string query = "SELECT amount FROM payments WHERE id = @id";
                    decimal totalPaid = 0;
                    using (var command = new NpgsqlCommand(query, connection)) {
                        command.Parameters.AddWithValue("id", id);
                        var result = await command.ExecuteScalarAsync();
                        if (result != null) {
                            totalPaid = Convert.ToDecimal(result);
                        }
                    }

                    // Insert or update payment
                    if (totalPaid == 0) {
                        string insertPaymentQuery = "INSERT INTO payments (id, amount) VALUES (@id, @paid)";
                        using (var paymentCommand = new NpgsqlCommand(insertPaymentQuery, connection)) {
                            paymentCommand.Parameters.AddWithValue("id", id);
                            paymentCommand.Parameters.AddWithValue("paid", paid);
                            await paymentCommand.ExecuteNonQueryAsync();
                        }
                    } else {
                        string updatePaymentQuery = "UPDATE payments SET amount = amount + @paid WHERE id = @id";
                        using (var paymentCommand = new NpgsqlCommand(updatePaymentQuery, connection)) {
                            paymentCommand.Parameters.AddWithValue("id", id);
                            paymentCommand.Parameters.AddWithValue("paid", paid);
                            await paymentCommand.ExecuteNonQueryAsync();
                        }
                        paid += totalPaid;
                    }

                    // If order exists, check if paid
                    bool isPaid = false;
                    string orderId = "";
                    string product = "";
                    decimal total;
                    string currency;
                    bool orderExists = false;
                    query = "SELECT id, product, total, currency FROM orders WHERE id = @id";
                    using (var orderCommand = new NpgsqlCommand(query, connection)) {
                        orderCommand.Parameters.AddWithValue("id", id);
                        using (var reader = await orderCommand.ExecuteReaderAsync()) {
                            if (await reader.ReadAsync()) {
                                orderId = reader.GetString(0);
                                product = reader.GetString(1);
                                total = reader.GetDecimal(2);
                                currency = reader.GetString(3);
                                isPaid = paid >= total;
                                orderExists = true;
                            }
                        }
                    }
                    // Update order status
                    if (!orderExists) {
                        string updateOrderQuery = "UPDATE orders SET isPaid = @isPaid WHERE id = @id";
                        using (var updateCommand = new NpgsqlCommand(updateOrderQuery, connection)) {
                            updateCommand.Parameters.AddWithValue("isPaid", isPaid);
                            updateCommand.Parameters.AddWithValue("id", orderId);
                            await updateCommand.ExecuteNonQueryAsync();
                        }
                        // If paid, print out
                        if (isPaid) {
                            Console.WriteLine($"Order {orderId} for {product} has been fully paid.");
                        }
                    }
                }
            } catch (NpgsqlException e) {
                Console.WriteLine($"Database error processing payment: {e.Message}");
            } catch (Exception e) {
                Console.WriteLine($"Unexpected error: {e.Message}");
            }
        }
    }
}