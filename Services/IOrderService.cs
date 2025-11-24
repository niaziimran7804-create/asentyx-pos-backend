using POS.Api.DTOs;

namespace POS.Api.Services
{
    public interface IOrderService
    {
        Task<IEnumerable<OrderDto>> GetAllOrdersAsync();
        Task<OrderDto?> GetOrderByIdAsync(int id);
        Task<OrderDto> CreateOrderAsync(CreateOrderDto createOrderDto);
        Task<bool> UpdateOrderAsync(int id, OrderDto orderDto);
        Task<bool> UpdateOrderStatusAsync(int id, UpdateOrderStatusDto updateDto, int userId);
        Task<int> BulkUpdateOrderStatusAsync(BulkUpdateOrderStatusDto bulkUpdateDto, int userId);
        Task<bool> DeleteOrderAsync(int id);
        Task<IEnumerable<CustomerSearchDto>> SearchCustomersAsync(string searchTerm);
    }
}


