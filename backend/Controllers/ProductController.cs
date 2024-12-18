﻿// Controllers/ProductController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Infraestrutura.Repositories.Data;
using Core.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.DTOs;
using System.IO;

namespace Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductController : ControllerBase
    {
        private readonly EficazContext _context;

        public ProductController(EficazContext context)
        {
            _context = context;
        }

        // GET: api/Product - Lista todos os produtos com imagens como Base64
        // Controllers/ProductController.cs
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProductDTO>>> GetProducts()
        {
            var products = await _context.Produtos
                .Include(p => p.Marca)
                .ToListAsync();

            var productDtos = products.Select(p => new ProductDTO
            {
                Id = p.Id,
                Nome = p.Nome,
                Preco = p.Preco,
                MarcaId = p.MarcaId,
                Imagem = p.Imagem, 
                ImagemHover = p.ImagemHover 
            }).ToList();

            return Ok(productDtos);
        }


        [HttpGet("{id}")]
        public async Task<ActionResult<ProductDTO>> GetProduct(int id)
        {
            var product = await _context.Produtos
                .Where(p => p.Id == id)
                .Select(p => new ProductDTO
                {
                    Id = p.Id,
                    Nome = p.Nome,
                    Preco = p.Preco,
                    MarcaId = p.MarcaId,
                    Imagem = p.Imagem, // Já está em Base64
                    ImagemHover = p.ImagemHover // Já está em Base64
                })
                .FirstOrDefaultAsync();

            if (product == null)
                return NotFound();

            return product;
        }



        // POST: api/Product - Adiciona um novo produto e armazena imagem como Base64
        [HttpPost]
        public async Task<ActionResult<ProductDTO>> CreateProduct([FromForm] ProductDTO productDto, IFormFile imagemArquivo, IFormFile imagemHoverArquivo)
        {
            var marca = await _context.Marcas.FindAsync(productDto.MarcaId);
            if (marca == null)
                return BadRequest("MarcaId inválido. A marca especificada não foi encontrada.");

            string base64Imagem = null;
            if (imagemArquivo != null && imagemArquivo.Length > 0)
                base64Imagem = await ConvertFileToBase64Async(imagemArquivo);

            string base64ImagemHover = null;
            if (imagemHoverArquivo != null && imagemHoverArquivo.Length > 0)
                base64ImagemHover = await ConvertFileToBase64Async(imagemHoverArquivo);

            var product = new Produto
            {
                Nome = productDto.Nome,
                Preco = productDto.Preco,
                MarcaId = productDto.MarcaId,
                Imagem = base64Imagem,
                ImagemHover = base64ImagemHover
            };

            _context.Produtos.Add(product);
            await _context.SaveChangesAsync();

            productDto.Id = product.Id;
            productDto.Imagem = base64Imagem;
            productDto.ImagemHover = base64ImagemHover;
            return CreatedAtAction(nameof(GetProduct), new { id = productDto.Id }, productDto);
        }

        // PUT: api/Product/5 - Atualiza um produto existente mantendo histórico
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProduct(int id, [FromForm] ProductDTO productDto, IFormFile? imagem, IFormFile? imagemHover)
        {
            var product = await _context.Produtos.FindAsync(id);
            if (product == null)
                return NotFound();

            // Atualize somente os campos que foram fornecidos
            if (!string.IsNullOrWhiteSpace(productDto.Nome))
                product.Nome = productDto.Nome;

            if (productDto.Preco.HasValue)
                product.Preco = productDto.Preco.Value;

            if (productDto.MarcaId.HasValue)
            {
                var marcaExists = await _context.Marcas.AnyAsync(m => m.Id == productDto.MarcaId.Value);
                if (!marcaExists)
                    return BadRequest("O MarcaId fornecido não existe.");
                product.MarcaId = productDto.MarcaId.Value;
            }

            if (imagem != null && imagem.Length > 0)
                product.Imagem = await ConvertFileToBase64Async(imagem);

            if (imagemHover != null && imagemHover.Length > 0)
                product.ImagemHover = await ConvertFileToBase64Async(imagemHover);

            _context.Entry(product).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(500, $"Erro ao atualizar o produto: {ex.InnerException?.Message}");
            }

            return NoContent();
        }






        // DELETE: api/Product/5 - Exclui um produto pelo ID
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _context.Produtos.FindAsync(id);
            if (product == null)
                return NotFound();

            _context.Produtos.Remove(product);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // Helper: Verifica se um produto existe pelo ID
        private bool ProductExists(int id)
        {
            return _context.Produtos.Any(e => e.Id == id);
        }

        // Helper: Converte arquivo para Base64
        private async Task<string> ConvertFileToBase64Async(IFormFile file)
        {
            using (var memoryStream = new MemoryStream())
            {
                await file.CopyToAsync(memoryStream);
                return Convert.ToBase64String(memoryStream.ToArray());
            }
        }

        // Torna o método estático para evitar capturas desnecessárias
        private static string ConvertToBase64(string relativePath)
        {
            var fullPath = Path.Combine("wwwroot", relativePath); // Constrói o caminho completo
            if (!System.IO.File.Exists(fullPath))
            {
                Console.WriteLine($"Arquivo não encontrado: {fullPath}");
                return null;
            }

            var fileBytes = System.IO.File.ReadAllBytes(fullPath); // Lê os bytes do arquivo
            return Convert.ToBase64String(fileBytes); // Retorna como Base64
        }
    }
}
