using System.Threading.Tasks;
using Thirdweb;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Web3 : MonoBehaviour
{
    private ThirdwebSDK sdk;

    public GameObject disconnectedState;

    public GameObject connectedState;

    public GameObject balanceText;

    public GameObject buyBlueNftButton;

    public GameObject buyRedNftButton;

    public static string selectedKart;

    void OnEnable()
    {
        sdk =
            new ThirdwebSDK("optimism-goerli",
                new ThirdwebSDK.Options()
                {
                    gasless =
                        new ThirdwebSDK.GaslessOptions()
                        {
                            openzeppelin =
                                new ThirdwebSDK.OZDefenderOptions()
                                {
                                    relayerUrl =
                                        "https://api.defender.openzeppelin.com/autotasks/c2e9a6ca-f2e8-4521-926b-1f9daec2dcb8/runs/webhook/826a5b67-d55d-49dc-8651-5db958ba22b2/DPtceJtayVGgKSDejaFnWk"
                                }
                        }
                });
    }

    private void LoadInfo()
    {
        ShowConnectedState();
        LoadBalance();
        DisplayButtonText("0", "2", buyBlueNftButton);
        DisplayButtonText("1", "3", buyRedNftButton);
    }

    private void ShowConnectedState()
    {
        disconnectedState.SetActive(false);
        connectedState.SetActive(true);
    }

    public async void ConnectWallet()
    {
        await EnsureWalletState();

        disconnectedState.SetActive(false);
        connectedState.SetActive(true);
        LoadInfo();
    }

    public async Task<string> EnsureWalletState()
    {
        string address =
            await sdk
                .wallet
                .Connect(new WalletConnection()
                {
                    provider = WalletProvider.CoinbaseWallet, // Use Coinbase Wallet
                    chainId = 420 // Switch the wallet Goerli network on connection
                });

        return address;
    }

    public async void LoadBalance()
    {
        var bal = await GetTokenDrop().ERC20.Balance();

        // Set balance text
        balanceText.GetComponent<TMPro.TextMeshProUGUI>().text =
            "Your balance: " + bal.displayValue + " " + bal.symbol;
    }

    private Contract GetEdition()
    {
        return sdk.GetContract("0xB46A62FaCfd6834eCEeeF666cFa1A976a911D6Fe");
    }

    private Contract GetTokenDrop()
    {
        return sdk.GetContract("0x4a9659d5E0d416Ce8B9a4336132012Af8db4c5AB");
    }

    private Marketplace GetMarketplace()
    {
        return sdk
            .GetContract("0x9b5283690D3161e61557b929C5846b1259c50693")
            .marketplace;
    }

    private async void DisplayButtonText(
        string tokenId,
        string listingId,
        GameObject button
    )
    {
        // Button text starts out as "Loading..."
        // First, check to see if the you own the NFT
        var owned = await GetEdition().ERC1155.GetOwned();

        // if owned contains a token with the same ID as the listing, then you own it
        bool ownsNft = owned.Exists(nft => nft.metadata.id == tokenId);
        if (ownsNft)
        {
            var text = button.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            text.text = "Drive Vehicle";

            // Set the on click to start the game by loading mane scene
            button
                .GetComponent<UnityEngine.UI.Button>()
                .onClick
                .AddListener(() =>
                {
                    selectedKart = tokenId;
                    SceneManager.LoadSceneAsync("MainScene");
                });
        }
        else
        {
            // Once we have the price, we update the text to the price
            var price = await GetMarketplace().GetListing(listingId);

            var text = button.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            text.text =
                "Buy:" +
                " " +
                price.buyoutCurrencyValuePerToken.displayValue +
                " " +
                price.buyoutCurrencyValuePerToken.symbol;

            // Set the onclick to buy the NFT
            button
                .GetComponent<UnityEngine.UI.Button>()
                .onClick
                .AddListener(async () =>
                {
                    await BuyItem(tokenId, listingId);
                    LoadBalance();
                });
        }
    }

    public async Task<TransactionResult>
    BuyItem(string tokenId, string listingId)
    {
        var result = await GetMarketplace().BuyListing(listingId, 1);

        if (result.isSuccessful())
        {
            // Remove the buy item listener
            var button = tokenId == "0" ? buyBlueNftButton : buyRedNftButton;
            button
                .GetComponent<UnityEngine.UI.Button>()
                .onClick
                .RemoveAllListeners();

            DisplayButtonText(tokenId,
            listingId,
            tokenId == "0" ? buyBlueNftButton : buyRedNftButton);
        }

        return result;
    }
}
