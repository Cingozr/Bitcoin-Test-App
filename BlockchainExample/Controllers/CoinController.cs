using BlockchainExample.Models;
using NBitcoin;
using QBitNinja.Client;
using QBitNinja.Client.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace BlockchainExample.Controllers
{
    public class CoinController : Controller
    {

        #region Fields
        static readonly BlockChainAPP db = new BlockChainAPP();
        static readonly Network network = Network.TestNet;
        #endregion

        #region Wallet List
        public ActionResult Index()
        {
            var walletList = db.Wallet.Where(t => t.IsActive).ToList();
            return View(walletList);
        }
        #endregion

        #region Create Wallet
        public ActionResult CreateWallet()
        {
            Key key = new Key();
            var privateKey = key.GetBitcoinSecret(network);
            var firstCreatePublicKey = key.PubKey.GetAddress(network); // birinci şekil public key oluşturma yöntemi
            var publicKey = privateKey.GetAddress(); // buda ikinci public key oluşturma yöntemi
            Wallet wallet = new Wallet
            {
                Balance = 0,
                IsActive = true,
                PublicKey = publicKey.ToString(),
                PrivateKey = privateKey.ToString(),
                TransactionId = string.Empty,
                TransactionUrl = string.Empty
            };
            db.Wallet.Add(wallet);
            var walletAddResult = db.SaveChanges();
            if (walletAddResult > 0)
            {
                return RedirectToAction("Index");
            }
            else
            {
                return View();
            }

        }
        #endregion

        #region Balance Check
        public JsonResult BalanceCheck(int Id)
        {

            var publicKeyBalance = db.Wallet.FirstOrDefault(t => t.IsActive && t.Id == Id);
            if (!string.IsNullOrWhiteSpace(publicKeyBalance.PublicKey))
            {

                Transaction transaction = new Transaction();
                QBitNinjaClient client = new QBitNinjaClient(network);
                var bitcoinSecret = new BitcoinSecret(publicKeyBalance.PrivateKey);
                var balanceModel = client.GetBalance(bitcoinSecret.GetAddress());
                if (balanceModel.Result.Operations.Count() == 0)
                {
                    return Json(new { State = false, Msg = "İşlem Hareketi Bulunamadı!" }, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    var unspendCoin = new List<Coin>();
                    var operationList = balanceModel.Result.Operations.Where(p => p.Confirmations >= 0).ToList();
                    foreach (var operation in operationList)
                    {
                        unspendCoin.AddRange(operation.ReceivedCoins.Select(coin => coin as Coin));
                    }
                    var balance = unspendCoin.Sum(x => x.Amount.ToDecimal(MoneyUnit.BTC));
                    var transId = operationList.Select(t => t.TransactionId.ToString()).FirstOrDefault();
                    if (publicKeyBalance.Balance == 0)
                    {

                        publicKeyBalance.PublicKey = bitcoinSecret.GetAddress().ToString();
                        publicKeyBalance.PrivateKey = bitcoinSecret.ToString();
                        publicKeyBalance.Balance = balance;
                        publicKeyBalance.TransactionId = transId;
                        publicKeyBalance.TransactionUrl = string.Format("<a href='http://tapi.qbit.ninja/transactions/{0}' target='_blank'>{1}</a>", transId, transId);
                        var walletBalanceUpdateResult = db.SaveChanges();
                        if (walletBalanceUpdateResult > 0)
                        {
                            return Json(new { State = true, publicKeyBalance.Id, publicKeyBalance.Balance, publicKeyBalance.TransactionUrl }, JsonRequestBehavior.AllowGet);
                        }
                        else
                        {
                            return Json(new { State = false, Msg = "Bakiye kaydetme sırasında hata oluştu!" }, JsonRequestBehavior.AllowGet);
                        }
                    }
                    else
                    {
                        return Json(new { State = true, publicKeyBalance.Id, publicKeyBalance.Balance, publicKeyBalance.TransactionUrl }, JsonRequestBehavior.AllowGet);
                    }
                }
            }
            else
            {
                return Json(new { State = false, Msg = "Cüzdan bulunamadı!" }, JsonRequestBehavior.AllowGet);
            }
        }
        #endregion

        #region Btc Send Methods

        #region Btc Send Detail
        public ActionResult BtcSendDetail(int Id)
        {
            ViewBag.Id = Id;
            var walletList = db.Wallet.Where(t => t.IsActive).ToList();
            return View(walletList);
        }
        #endregion

        #region First Method
        //Uzun Uzadı
        public ActionResult FirstMethodBtcSend(int gondericiId, int aliciId, string miktar)
        {
            Transaction transaction = new Transaction();
            QBitNinjaClient client = new QBitNinjaClient(network);

            var senderKey = db.Wallet.FirstOrDefault(t => t.IsActive && t.Id == gondericiId);
            if (senderKey != null)
            {
                var senderPrivateKey = new BitcoinSecret(senderKey.PrivateKey);
                var receivedCoins = client.GetTransaction(uint256.Parse(senderKey.TransactionId)).Result.ReceivedCoins;
                OutPoint outPointToSpend = null;
                foreach (var coin in receivedCoins)
                {
                    if (coin.TxOut.ScriptPubKey == new BitcoinSecret(senderKey.PrivateKey).ScriptPubKey)
                    {
                        outPointToSpend = coin.Outpoint;
                    }
                }

                transaction.Inputs.Add(new TxIn
                {
                    PrevOut = outPointToSpend
                });

                #region Nereye
                var gidecekKey = db.Wallet.FirstOrDefault(t => t.IsActive && t.Id == aliciId).PublicKey;

                #region Hesaplama
                var gidecekMiktar = Money.Parse(miktar);
                var minerFee = new Money(0.00007m, MoneyUnit.BTC);
                var neKadarBtcVar = (Money)receivedCoins[(int)outPointToSpend.N].Amount;
                var neKadarDonecek = neKadarBtcVar - gidecekMiktar - minerFee;
                #endregion

                transaction.Outputs.Add(new TxOut
                {
                    Value = gidecekMiktar,
                    ScriptPubKey = BitcoinAddress.Create(gidecekKey).ScriptPubKey
                });

                transaction.Outputs.Add(new TxOut
                {
                    Value = neKadarDonecek,
                    ScriptPubKey = senderPrivateKey.ScriptPubKey
                });

                transaction.Inputs[0].ScriptSig = senderPrivateKey.ScriptPubKey;
                #pragma warning disable CS0618 // Type or member is obsolete
                transaction.Sign(senderPrivateKey, false);
                #pragma warning restore CS0618 // Type or member is obsolete

                BroadcastResponse broadcastResponse = client.Broadcast(transaction).Result;
                if (broadcastResponse.Success)
                {
                    string hex = transaction.ToHex();
                }
                else
                {

                }

                #endregion


            }

            return Json(JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region Second Method
        //Kısa Btc Send - Using Transaction Builder 
        [HttpPost]
        public JsonResult SecondMethodBtcSend(int gondericiId, int aliciId, string miktar)
        {
            var gondericiModel = db.Wallet.Where(t => t.IsActive && t.Id == gondericiId).FirstOrDefault();
            if (gondericiModel != null)
            {
                Transaction transaction = new Transaction();
                TransactionBuilder transactionBuilder = new TransactionBuilder();
                QBitNinjaClient client = new QBitNinjaClient(network);
                var txRepo = new NoSqlTransactionRepository();
                var sendAmount = Money.Parse(miktar);
                var senderKey = new BitcoinSecret(gondericiModel.PrivateKey);
                var receivedPublicKey = BitcoinAddress.Create(db.Wallet.FirstOrDefault(t => t.IsActive && t.Id == aliciId).PublicKey);
                var balance = client.GetBalance(senderKey.GetAddress(), true).GetAwaiter().GetResult().Operations.SelectMany(o => o.ReceivedCoins).ToArray();

                transaction = transactionBuilder
                                .AddCoins(balance)
                                .AddKeys(senderKey)
                                .Send(receivedPublicKey, sendAmount)
                                .SendFees("0.0004")
                                .SetChange(senderKey)
                                .BuildTransaction(sign: true);

                transactionBuilder.SignTransaction(transaction);
                transactionBuilder.Verify(transaction);

                BroadcastResponse broadcastResponse = client.Broadcast(transaction).Result;
                if (!broadcastResponse.Success)
                {
                    Console.Error.WriteLine("ErrorCode: " + broadcastResponse.Error.ErrorCode);
                    Console.Error.WriteLine("Error message: " + broadcastResponse.Error.Reason);
                }
                else
                {
                    Console.WriteLine("Success! You can check out the hash of the transaciton in any block explorer:");
                    Console.WriteLine(transaction.GetHash());
                }
                //var result = client.Broadcast(transaction);
                //if (transactionBuilder.Verify(transaction))
                //{
                //    txRepo.Put(transaction.GetHash(), transaction);
                //    var hexResult = transaction.ToHex();
                //}
                return Json(new { State = false, Msg = "İşlem Başarılı" }, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return Json(new { State = true, Msg = "İşlem Başarılı" }, JsonRequestBehavior.AllowGet);
            }
        }
        #endregion

        #endregion


        #region Wallet Delete
        public ActionResult WalletDelete(int Id)
        {
            var walletResponse = db.Wallet.Where(t => t.IsActive && t.Id == Id).FirstOrDefault();
            if (walletResponse != null)
            {
                walletResponse.IsActive = false;
                var saveResult = db.SaveChanges();
                if (saveResult > 0)
                {
                    return RedirectToAction("Index");
                }
                else
                {
                    return Json(new { Msg = "Silme sırasında hata oluştu" }, JsonRequestBehavior.AllowGet);
                }
            }
            else
            {
                return Json(new { Msg = "Cüzdan bulunamadı" }, JsonRequestBehavior.AllowGet);
            }
        }
        #endregion
    }
}